using TicketSystem.Api.Realtime;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Features.Chat;

public sealed class ChatRealtimeNotifier
{
    private readonly RealtimeNotificationClient realtimeNotificationClient;

    public ChatRealtimeNotifier(RealtimeNotificationClient realtimeNotificationClient)
    {
        this.realtimeNotificationClient = realtimeNotificationClient;
    }

    public Task NotifyChangedAsync(Guid ticketId, Guid chatSessionId, Guid customerId)
    {
        return realtimeNotificationClient.NotifyAsync(new RealtimeEventRequest(ChatRealtimeEvents.Changed, TicketId: ticketId, ChatSessionId: chatSessionId, CustomerId: customerId));
    }
}
