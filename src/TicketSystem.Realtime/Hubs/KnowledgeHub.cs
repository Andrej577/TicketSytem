using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TicketSystem.Realtime.Hubs;

[Authorize(Roles = "Operator,Administrator")]
public sealed class KnowledgeHub : Hub
{
}
