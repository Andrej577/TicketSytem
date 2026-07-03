namespace TicketSystem.Shared.DTO;

public sealed class TicketDTO
{
    public Guid Id { get; set; }

    public long TicketNumber { get; set; }

    public Guid ChatSessionId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? OperatorId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }
}
