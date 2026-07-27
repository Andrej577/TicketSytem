using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Web.Authentication;

public static class WebAuthenticationEndpoints
{
    private static readonly IReadOnlyDictionary<string, string> QuickLoginPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@ticketsystem.local"] = "admin",
        ["customer1@ticketsystem.local"] = "test123",
        ["customer2@ticketsystem.local"] = "test123",
        ["support1@ticketsystem.local"] = "test123",
        ["support2@ticketsystem.local"] = "test123"
    };

    public static IEndpointRouteBuilder MapWebAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync)
            .AllowAnonymous()
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        endpoints.MapPost("/auth/quick-login", QuickLoginAsync)
            .AllowAnonymous()
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        endpoints.MapPost("/auth/logout", LogoutAsync)
            .RequireAuthorization()
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

        return endpoints;
    }

    private static async Task<IResult> LoginAsync([FromForm] LoginForm form, HttpContext httpContext, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
    {
        return await SignInAsync(form.Email, form.Password, form.ReturnUrl, httpContext, httpClientFactory, cancellationToken);
    }

    private static async Task<IResult> QuickLoginAsync([FromForm] QuickLoginForm form, HttpContext httpContext, IConfiguration configuration, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Authentication:EnableQuickLogin") || !QuickLoginPasswords.TryGetValue(form.Email, out var password))
        {
            return Results.NotFound();
        }

        return await SignInAsync(form.Email, password, form.ReturnUrl, httpContext, httpClientFactory, cancellationToken);
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/login");
    }

    private static async Task<IResult> SignInAsync(string email, string password, string? returnUrl, HttpContext httpContext, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("TicketSystemApi");
            using var response = await client.PostAsJsonAsync("api/auth/login", new ApiLoginRequest(email, password), cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return LoginRedirect("invalid", returnUrl);
            }

            if (!response.IsSuccessStatusCode)
            {
                return LoginRedirect("unavailable", returnUrl);
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<ApiLoginResponse>(cancellationToken);
            if (loginResponse is null)
            {
                return LoginRedirect("unavailable", returnUrl);
            }

            var role = ((AppUserType)loginResponse.UserTypeId).ToString();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()),
                new Claim(ClaimTypes.Name, $"{loginResponse.FirstName} {loginResponse.LastName}"),
                new Claim(ClaimTypes.GivenName, loginResponse.FirstName),
                new Claim(ClaimTypes.Surname, loginResponse.LastName),
                new Claim(ClaimTypes.Email, loginResponse.Email),
                new Claim(ClaimTypes.Role, role),
                new Claim(TicketSystemClaimTypes.ApiAccessToken, loginResponse.AccessToken)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = loginResponse.ExpiresAt
            };
            properties.StoreTokens(
            [
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = loginResponse.AccessToken
                }
            ]);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
            return Results.LocalRedirect(GetDestination(returnUrl, role));
        }
        catch (HttpRequestException)
        {
            return LoginRedirect("unavailable", returnUrl);
        }
        catch (JsonException)
        {
            return LoginRedirect("unavailable", returnUrl);
        }
        catch (TaskCanceledException)
        {
            return LoginRedirect("unavailable", returnUrl);
        }
    }

    private static IResult LoginRedirect(string error, string? returnUrl)
    {
        return Results.LocalRedirect($"/login?error={error}{returnUrl.ToReturnUrlQuery()}");
    }

    private static string GetDestination(string? returnUrl, string role)
    {
        var localReturnUrl = returnUrl ?? string.Empty;
        var isLocalReturnUrl = Uri.IsWellFormedUriString(localReturnUrl, UriKind.Relative)
            && localReturnUrl.StartsWith('/')
            && (localReturnUrl.Length == 1 || localReturnUrl[1] is not '/' and not '\\');
        if (isLocalReturnUrl && role == nameof(AppUserType.Administrator))
        {
            return localReturnUrl;
        }

        if (isLocalReturnUrl && role == nameof(AppUserType.Operator)
            && (localReturnUrl.StartsWith("/tickets", StringComparison.OrdinalIgnoreCase) || localReturnUrl.StartsWith("/knowledge-base", StringComparison.OrdinalIgnoreCase) || localReturnUrl.StartsWith("/settings", StringComparison.OrdinalIgnoreCase)))
        {
            return localReturnUrl;
        }

        if (isLocalReturnUrl && role == nameof(AppUserType.Customer)
            && (localReturnUrl.StartsWith("/tickets", StringComparison.OrdinalIgnoreCase) || localReturnUrl.StartsWith("/settings", StringComparison.OrdinalIgnoreCase)))
        {
            return localReturnUrl;
        }

        return role == nameof(AppUserType.Administrator) ? "/" : "/tickets";
    }

    private static string ToReturnUrlQuery(this string? returnUrl)
    {
        return string.IsNullOrWhiteSpace(returnUrl) ? string.Empty : $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
