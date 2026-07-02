namespace TicketSystem.Realtime.Models;

public sealed class ChatMessage
{
    public required Guid SessionId { get; init; }

    public required Guid UserId { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}
