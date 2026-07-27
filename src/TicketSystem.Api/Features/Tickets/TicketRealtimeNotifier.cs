using TicketSystem.Api.Realtime;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Features.Tickets;

public sealed class TicketRealtimeNotifier
{
    private readonly RealtimeNotificationClient realtimeNotificationClient;

    public TicketRealtimeNotifier(RealtimeNotificationClient realtimeNotificationClient)
    {
        this.realtimeNotificationClient = realtimeNotificationClient;
    }

    public Task NotifyCreatedTicketAsync(Guid ticketId)
    {
        return realtimeNotificationClient.NotifyAsync(new RealtimeEventRequest(TicketRealtimeEvents.Created, EntityId: ticketId));
    }

    public Task NotifyUpdatedTicketAsync(Guid ticketId)
    {
        return realtimeNotificationClient.NotifyAsync(new RealtimeEventRequest(TicketRealtimeEvents.Updated, EntityId: ticketId));
    }

    public Task NotifyDeletedTicketAsync(Guid ticketId)
    {
        return realtimeNotificationClient.NotifyAsync(new RealtimeEventRequest(TicketRealtimeEvents.Deleted, EntityId: ticketId));
    }
}
