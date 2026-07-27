using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using TicketSystem.Realtime.Hubs;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Realtime.Internal;

public static class InternalRealtimeEndpoints
{
    public static IEndpointRouteBuilder MapInternalRealtimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(RealtimeInternalApi.EventsPath, PublishAsync).AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> PublishAsync(RealtimeEventRequest request, HttpRequest httpRequest, InternalRealtimeOptions options, IHubContext<AppUserHub> appUserHub, IHubContext<ChatHub> chatHub, IHubContext<KnowledgeHub> knowledgeHub, IHubContext<TicketHub> ticketHub, CancellationToken cancellationToken)
    {
        if (!HasValidKey(httpRequest, options.Key))
        {
            return Results.Unauthorized();
        }

        switch (request.EventName)
        {
            case TicketRealtimeEvents.Created:
            case TicketRealtimeEvents.Updated:
            case TicketRealtimeEvents.Deleted:
                if (request.EntityId is not Guid ticketId)
                {
                    return Results.BadRequest();
                }

                await ticketHub.Clients.All.SendAsync(request.EventName, ticketId, cancellationToken);
                break;

            case AppUserRealtimeEvents.Changed:
                await appUserHub.Clients.All.SendAsync(AppUserRealtimeEvents.Changed, cancellationToken);
                break;

            case KnowledgeRealtimeEvents.Changed:
                await knowledgeHub.Clients.All.SendAsync(KnowledgeRealtimeEvents.Changed, cancellationToken);
                break;

            case ChatRealtimeEvents.Changed:
                if (request.TicketId is not Guid chatTicketId || request.ChatSessionId is not Guid chatSessionId || request.CustomerId is not Guid customerId)
                {
                    return Results.BadRequest();
                }

                await Task.WhenAll(
                    chatHub.Clients.Group(ChatHub.StaffGroup).SendAsync(ChatRealtimeEvents.Changed, chatTicketId, chatSessionId, cancellationToken),
                    chatHub.Clients.User(customerId.ToString()).SendAsync(ChatRealtimeEvents.Changed, chatTicketId, chatSessionId, cancellationToken));
                break;

            default:
                return Results.BadRequest();
        }

        return Results.NoContent();
    }

    private static bool HasValidKey(HttpRequest request, string expectedKey)
    {
        var providedKey = request.Headers[RealtimeInternalApi.KeyHeaderName].ToString();
        var providedKeyBytes = Encoding.UTF8.GetBytes(providedKey);
        var expectedKeyBytes = Encoding.UTF8.GetBytes(expectedKey);
        return providedKeyBytes.Length == expectedKeyBytes.Length && CryptographicOperations.FixedTimeEquals(providedKeyBytes, expectedKeyBytes);
    }
}
