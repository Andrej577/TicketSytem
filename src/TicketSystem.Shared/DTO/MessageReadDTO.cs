namespace TicketSystem.Shared.DTO;

public sealed class MessageReadDTO
{
    public Guid MessageId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset ReadAt { get; set; }
}
