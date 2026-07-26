namespace TicketSystem.Api.Features.Tickets;

public sealed record CreateTicketRequest(string Title, string Content, Guid? CustomerId = null, Guid? OperatorId = null, short? StatusId = null, short? PriorityId = null, DateTimeOffset? ClosedAt = null);

public sealed record UpdateTicketRequest(Guid CustomerId, Guid? OperatorId, string Title, string Content, short StatusId, short PriorityId, DateTimeOffset? ClosedAt);
