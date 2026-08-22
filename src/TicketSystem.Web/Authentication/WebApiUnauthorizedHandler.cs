using Microsoft.AspNetCore.Components;
using TicketSystem.Client.Authentication;

namespace TicketSystem.Web.Authentication;

public sealed class WebApiUnauthorizedHandler(NavigationManager navigationManager) : IApiUnauthorizedHandler
{
    public Task HandleAsync(CancellationToken cancellationToken = default)
    {
        navigationManager.NavigateTo("/login?error=expired", true);
        return Task.CompletedTask;
    }
}
