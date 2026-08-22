using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketSystem.Client.Authentication;

namespace TicketSystem.Client;

public sealed class TicketSystemApiClient(HttpClient httpClient, IAccessTokenProvider accessTokenProvider, IApiUnauthorizedHandler unauthorizedHandler)
{
    public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Get, requestUri, null, cancellationToken);
    }

    public async Task<T?> GetFromJsonAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Post, requestUri, JsonContent.Create(value), cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Post, requestUri, content, cancellationToken);
    }

    public Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Put, requestUri, JsonContent.Create(value), cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Delete, requestUri, null, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string requestUri, HttpContent? content, CancellationToken cancellationToken)
    {
        var accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken)
            ?? throw new InvalidOperationException("The authenticated user does not have an API access token.");
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await unauthorizedHandler.HandleAsync(cancellationToken);
        }

        return response;
    }
}
