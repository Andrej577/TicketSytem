namespace TicketSystem.Shared.DTO;

public class MediaFileDTO
{
    public Guid Id { get; set; }

    public Guid ChatSessionId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
