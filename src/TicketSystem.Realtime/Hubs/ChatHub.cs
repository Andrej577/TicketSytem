using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Realtime.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    public const string StaffGroup = "chat-staff";

    public override async Task OnConnectedAsync()
    {
        if (Context.User!.IsInRole(nameof(AppUserType.Operator)) || Context.User.IsInRole(nameof(AppUserType.Administrator)))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
        }

        await base.OnConnectedAsync();
    }
}
