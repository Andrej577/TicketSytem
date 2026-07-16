namespace TicketSystem.Shared.DTO;

public sealed class ChatSessionDTO
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? OperatorId { get; set; }

    public string? Title { get; set; }

    public short StatusId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }
}
