using Microsoft.AspNetCore.SignalR;
using TicketSystem.Realtime.Models;

namespace TicketSystem.Realtime.Hubs;

public sealed class ChatHub : Hub
{
    public Task JoinSession(Guid sessionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(sessionId));
    }

    public Task LeaveSession(Guid sessionId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(sessionId));
    }

    public Task SendMessage(ChatMessage message)
    {
        return Clients.Group(GetGroupName(message.SessionId))
            .SendAsync("messageReceived", message);
    }

    private static string GetGroupName(Guid sessionId)
    {
        return $"chat-session:{sessionId}";
    }
}
