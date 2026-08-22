using TicketSystem.Api.Features.AppUsers;
using TicketSystem.DAL.AppUsers;
using TicketSystem.Shared.Authentication;

namespace TicketSystem.Api.Features.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> Login(LoginRequest request, AppUserDAL appUserDAL, JwtTokenService jwtTokenService, CancellationToken cancellationToken)
    {
        var appUser = await appUserDAL.GetForLoginAsync(request.Email, cancellationToken);

        if (appUser is null || !PasswordHasher.Verify(request.Password, appUser.PasswordHash))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(jwtTokenService.Create(appUser));
    }
}
