namespace TicketSystem.Api.Features.Chat;

public sealed record SendMessageRequest(string Content, bool AssignToCurrentUser = false);
