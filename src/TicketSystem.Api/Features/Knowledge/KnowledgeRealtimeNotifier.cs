using Microsoft.AspNetCore.SignalR;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Features.Knowledge;

public sealed class KnowledgeRealtimeNotifier
{
    private readonly IHubContext<KnowledgeHub> knowledgeHub;
    private readonly ILogger<KnowledgeRealtimeNotifier> logger;

    public KnowledgeRealtimeNotifier(IHubContext<KnowledgeHub> knowledgeHub, ILogger<KnowledgeRealtimeNotifier> logger)
    {
        this.knowledgeHub = knowledgeHub;
        this.logger = logger;
    }

    public async Task NotifyChangedAsync()
    {
        try
        {
            await knowledgeHub.Clients.All.SendAsync(KnowledgeRealtimeEvents.Changed);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Changed knowledge articles could not be published through SignalR.");
        }
    }
}
