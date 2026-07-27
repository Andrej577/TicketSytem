using TicketSystem.Api.Realtime;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Features.AppUsers;

public sealed class AppUserRealtimeNotifier
{
    private readonly RealtimeNotificationClient realtimeNotificationClient;

    public AppUserRealtimeNotifier(RealtimeNotificationClient realtimeNotificationClient)
    {
        this.realtimeNotificationClient = realtimeNotificationClient;
    }

    public Task NotifyChangedAsync()
    {
        return realtimeNotificationClient.NotifyAsync(new RealtimeEventRequest(AppUserRealtimeEvents.Changed));
    }
}
