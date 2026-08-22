using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using TicketSystem.Client;

namespace TicketSystem.Maui.Authentication;

public sealed class MauiMediaFileDownloadService(TicketSystemApiClient apiClient) : IMediaFileDownloadService
{
    public async Task DownloadAsync(Guid ticketId, Guid mediaFileId, string fileName, CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.GetAsync($"api/tickets/{ticketId}/chat/media/{mediaFileId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var safeFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(FileSystem.Current.CacheDirectory, safeFileName);
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(filePath);
        await source.CopyToAsync(destination, cancellationToken);
        await Launcher.Default.OpenAsync(new OpenFileRequest(safeFileName, new ReadOnlyFile(filePath)));
    }
}
