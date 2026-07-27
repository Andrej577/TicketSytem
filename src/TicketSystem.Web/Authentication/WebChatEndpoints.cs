using System.Net.Http.Headers;

namespace TicketSystem.Web.Authentication;

public static class WebChatEndpoints
{
    public static IEndpointRouteBuilder MapWebChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/downloads/tickets/{ticketId:guid}/chat/media/{mediaFileId:guid}", DownloadMediaFileAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> DownloadMediaFileAsync(Guid ticketId, Guid mediaFileId, HttpContext httpContext, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
    {
        var accessToken = httpContext.User.FindFirst(TicketSystemClaimTypes.ApiAccessToken)!.Value;
        var client = httpClientFactory.CreateClient("TicketSystemApi");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/tickets/{ticketId}/chat/media/{mediaFileId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Results.NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "attachment";
        return Results.File(content, contentType, fileName);
    }
}
