namespace TicketSystem.Api.Features.Authentication;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, Guid UserId, string Email, int UserTypeId);
