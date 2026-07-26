using Microsoft.AspNetCore.SignalR;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Features.Tickets;

public sealed class TicketRealtimeNotifier
{
    private readonly IHubContext<TicketHub> ticketHub;
    private readonly ILogger<TicketRealtimeNotifier> logger;

    public TicketRealtimeNotifier(IHubContext<TicketHub> ticketHub, ILogger<TicketRealtimeNotifier> logger)
    {
        this.ticketHub = ticketHub;
        this.logger = logger;
    }

    public async Task NotifyCreatedTicketAsync(Guid ticketId)
    {
        try
        {
            await ticketHub.Clients.All.SendAsync(TicketRealtimeEvents.Created, ticketId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Created ticket {TicketId} could not be published through SignalR.", ticketId);
        }
    }
}
