namespace TicketSystem.Api.Features.Tickets;

public sealed record CreateTicketRequest(Guid CustomerId, Guid? OperatorId, string Title, string Content, short StatusId, short PriorityId, DateTimeOffset? ClosedAt);

public sealed record UpdateTicketRequest(Guid CustomerId, Guid? OperatorId, string Title, string Content, short StatusId, short PriorityId, DateTimeOffset? ClosedAt);
