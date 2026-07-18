namespace TicketSystem.Shared.DTO;

public sealed class TicketDTO
{
    public Guid Id { get; set; }

    public long TicketNumber { get; set; }

    public Guid? ChatSessionId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? OperatorId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public short StatusId { get; set; }

    public short PriorityId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public bool IsDeleted { get; set; }

    public Guid UpdatedByUserId { get; set; }
}
