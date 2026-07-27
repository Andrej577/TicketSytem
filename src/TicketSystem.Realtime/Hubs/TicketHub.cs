using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TicketSystem.Realtime.Hubs;

[Authorize]
public sealed class TicketHub : Hub
{
}
