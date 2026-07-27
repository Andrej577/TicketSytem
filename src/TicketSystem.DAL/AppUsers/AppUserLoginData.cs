namespace TicketSystem.DAL.AppUsers;

public sealed class AppUserLoginData
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int UserTypeId { get; set; }
}
