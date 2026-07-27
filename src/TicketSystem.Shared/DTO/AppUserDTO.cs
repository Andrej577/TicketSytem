namespace TicketSystem.Shared.DTO;

public class AppUserDTO
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int UserTypeId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid UpdatedByUserId { get; set; }
}
