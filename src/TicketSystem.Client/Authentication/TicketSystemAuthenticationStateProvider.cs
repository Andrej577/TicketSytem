using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TicketSystem.Shared.Authentication;

namespace TicketSystem.Client.Authentication;

public sealed class TicketSystemAuthenticationStateProvider(IAuthenticationSession session) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var loginResponse = await session.GetAsync();
        return loginResponse is null || loginResponse.ExpiresAt <= DateTimeOffset.UtcNow
            ? new AuthenticationState(Anonymous)
            : new AuthenticationState(TicketSystemClaimsPrincipalFactory.Create(loginResponse, "TicketSystem"));
    }

    public async Task SignInAsync(LoginResponse loginResponse, CancellationToken cancellationToken = default)
    {
        await session.SetAsync(loginResponse, cancellationToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await session.ClearAsync(cancellationToken);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

}
