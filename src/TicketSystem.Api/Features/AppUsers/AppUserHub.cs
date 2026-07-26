using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Api.Features.AppUsers;

[Authorize(Roles = nameof(AppUserType.Administrator))]
public sealed class AppUserHub : Hub
{
}
