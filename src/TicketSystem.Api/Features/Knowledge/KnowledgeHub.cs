using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TicketSystem.Api.Features.Knowledge;

[Authorize(Roles = "Operator,Administrator")]
public sealed class KnowledgeHub : Hub
{
}
