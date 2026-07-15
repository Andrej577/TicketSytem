namespace TicketSystem.Api.Features.AppUsers;

public sealed record CreateAppUserRequest(string? Email, string Password, int UserTypeId);

public sealed record UpdateAppUserRequest(string? Email, string? Password);
