using TicketSystem.Api.Realtime;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Features.Knowledge;

public sealed class KnowledgeRealtimeNotifier
{
    private readonly RealtimeNotificationClient realtimeNotificationClient;

    public KnowledgeRealtimeNotifier(RealtimeNotificationClient realtimeNotificationClient)
    {
        this.realtimeNotificationClient = realtimeNotificationClient;
    }

    public Task NotifyChangedAsync()
    {
        return realtimeNotificationClient.NotifyAsync(new RealtimeEventRequest(KnowledgeRealtimeEvents.Changed));
    }
}
