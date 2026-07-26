using Microsoft.AspNetCore.SignalR;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Features.AppUsers;

public sealed class AppUserRealtimeNotifier
{
    private readonly IHubContext<AppUserHub> appUserHub;
    private readonly ILogger<AppUserRealtimeNotifier> logger;

    public AppUserRealtimeNotifier(IHubContext<AppUserHub> appUserHub, ILogger<AppUserRealtimeNotifier> logger)
    {
        this.appUserHub = appUserHub;
        this.logger = logger;
    }

    public async Task NotifyChangedAsync()
    {
        try
        {
            await appUserHub.Clients.All.SendAsync(AppUserRealtimeEvents.Changed);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Changed app users could not be published through SignalR.");
        }
    }
}
