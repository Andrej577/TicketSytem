using Microsoft.AspNetCore.Components;
using TicketSystem.Client;

namespace TicketSystem.Web.Authentication;

public sealed class WebMediaFileDownloadService(NavigationManager navigationManager) : IMediaFileDownloadService
{
    public Task DownloadAsync(Guid ticketId, Guid mediaFileId, string fileName, CancellationToken cancellationToken = default)
    {
        navigationManager.NavigateTo($"/downloads/tickets/{ticketId}/chat/media/{mediaFileId}", true);
        return Task.CompletedTask;
    }
}
