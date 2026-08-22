using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Client.Authentication;
using TicketSystem.Shared.Authentication;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Web.Authentication;

public static class WebAuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapWebAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync)
            .AllowAnonymous()
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        endpoints.MapPost("/auth/logout", (Delegate)LogoutAsync)
            .RequireAuthorization()
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

        return endpoints;
    }

    private static async Task<IResult> LoginAsync([FromForm] LoginForm form, HttpContext httpContext, TicketSystemAuthenticationClient authenticationClient, CancellationToken cancellationToken)
    {
        return await SignInAsync(form.Email, form.Password, form.ReturnUrl, httpContext, authenticationClient, cancellationToken);
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/login");
    }

    private static async Task<IResult> SignInAsync(string email, string password, string? returnUrl, HttpContext httpContext, TicketSystemAuthenticationClient authenticationClient, CancellationToken cancellationToken)
    {
        try
        {
            var loginResponse = await authenticationClient.LoginAsync(new LoginRequest(email, password), cancellationToken);
            if (loginResponse is null)
            {
                return LoginRedirect("invalid", returnUrl);
            }

            var role = ((AppUserType)loginResponse.UserTypeId).ToString();
            var principal = TicketSystemClaimsPrincipalFactory.Create(loginResponse, CookieAuthenticationDefaults.AuthenticationScheme);
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
            && (localReturnUrl.StartsWith("/tickets", StringComparison.OrdinalIgnoreCase) || localReturnUrl.StartsWith("/knowledge-base", StringComparison.OrdinalIgnoreCase)))
        {
            return localReturnUrl;
        }

        if (isLocalReturnUrl && role == nameof(AppUserType.Customer)
            && localReturnUrl.StartsWith("/tickets", StringComparison.OrdinalIgnoreCase))
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
