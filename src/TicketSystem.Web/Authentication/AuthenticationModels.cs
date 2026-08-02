namespace TicketSystem.Web.Authentication;

public sealed record ApiLoginRequest(string Email, string Password);

public sealed record ApiLoginResponse(string AccessToken, DateTimeOffset ExpiresAt, Guid UserId, string Email, string FirstName, string LastName, int UserTypeId);

public sealed class LoginForm
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
