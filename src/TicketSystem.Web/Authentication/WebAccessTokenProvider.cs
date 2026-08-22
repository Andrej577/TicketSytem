using Microsoft.AspNetCore.Components.Authorization;
using TicketSystem.Client.Authentication;
using TicketSystem.Shared.Authentication;

namespace TicketSystem.Web.Authentication;

public sealed class WebAccessTokenProvider(AuthenticationStateProvider authenticationStateProvider) : IAccessTokenProvider
{
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return authenticationState.User.FindFirst(TicketSystemClaimTypes.ApiAccessToken)?.Value;
    }
}
