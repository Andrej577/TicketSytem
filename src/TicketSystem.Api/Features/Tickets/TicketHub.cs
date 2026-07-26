using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TicketSystem.Api.Features.Tickets;

[Authorize]
public sealed class TicketHub : Hub
{
}
