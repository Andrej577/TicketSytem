using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace TicketSystem.Web.Authentication;

public sealed class TicketSystemApiClient
{
    private readonly AuthenticationStateProvider authenticationStateProvider;
    private readonly HttpClient httpClient;
    private readonly NavigationManager navigationManager;

    public TicketSystemApiClient(AuthenticationStateProvider authenticationStateProvider, IHttpClientFactory httpClientFactory, NavigationManager navigationManager)
    {
        this.authenticationStateProvider = authenticationStateProvider;
        this.navigationManager = navigationManager;
        httpClient = httpClientFactory.CreateClient("TicketSystemApi");
    }

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
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var accessToken = authenticationState.User.FindFirst(TicketSystemClaimTypes.ApiAccessToken)?.Value
            ?? throw new InvalidOperationException("The authenticated user does not have an API access token.");
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            navigationManager.NavigateTo("/login?error=expired", true);
        }

        return response;
    }
}
