namespace TicketSystem.Shared.DTO;

public sealed class CustomerDTO
{
    public Guid Id { get; set; }

    public Guid AppUserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
