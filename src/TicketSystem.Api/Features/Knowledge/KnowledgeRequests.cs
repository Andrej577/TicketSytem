namespace TicketSystem.Api.Features.Knowledge;

public sealed record CreateKnowledgeRequest(string Title, string Content, Guid? CategoryId, short StatusId);

public sealed record UpdateKnowledgeRequest(string Title, string Content, Guid? CategoryId, short StatusId);
