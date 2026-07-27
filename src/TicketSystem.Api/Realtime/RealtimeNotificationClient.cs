using System.Net.Http.Json;
using TicketSystem.Shared.Realtime;

namespace TicketSystem.Api.Realtime;

public sealed class RealtimeNotificationClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<RealtimeNotificationClient> logger;
    private readonly string? baseUrl;
    private readonly string? internalKey;

    public RealtimeNotificationClient(HttpClient httpClient, IConfiguration configuration, ILogger<RealtimeNotificationClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        baseUrl = configuration["Realtime:BaseUrl"];
        internalKey = configuration["Realtime:InternalKey"];
    }

    public async Task NotifyAsync(RealtimeEventRequest realtimeEvent)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(internalKey))
        {
            logger.LogWarning("Realtime event {RealtimeEvent} was not published because the Realtime configuration is missing.", realtimeEvent.EventName);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}{RealtimeInternalApi.EventsPath}");
            request.Headers.Add(RealtimeInternalApi.KeyHeaderName, internalKey);
            request.Content = JsonContent.Create(realtimeEvent);
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Realtime event {RealtimeEvent} could not be published. Realtime returned HTTP {StatusCode}.", realtimeEvent.EventName, (int)response.StatusCode);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Realtime event {RealtimeEvent} could not be published.", realtimeEvent.EventName);
        }
    }
}
