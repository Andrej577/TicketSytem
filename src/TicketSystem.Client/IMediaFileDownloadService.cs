namespace TicketSystem.Client;

public interface IMediaFileDownloadService
{
    Task DownloadAsync(Guid ticketId, Guid mediaFileId, string fileName, CancellationToken cancellationToken = default);
}
