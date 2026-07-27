namespace TicketSystem.Shared.DTO;

public class MessageDTO
{
    public Guid Id { get; set; }

    public Guid ChatSessionId { get; set; }

    public Guid? TicketId { get; set; }

    public Guid SenderId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }
}
