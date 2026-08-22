using Microsoft.AspNetCore.Components;
using TicketSystem.Client.Authentication;

namespace TicketSystem.Maui.Authentication;

public sealed class MauiApiUnauthorizedHandler(TicketSystemAuthenticationStateProvider authenticationStateProvider, NavigationManager navigationManager) : IApiUnauthorizedHandler
{
    public async Task HandleAsync(CancellationToken cancellationToken = default)
    {
        await authenticationStateProvider.SignOutAsync(cancellationToken);
        navigationManager.NavigateTo("/login");
    }
}
