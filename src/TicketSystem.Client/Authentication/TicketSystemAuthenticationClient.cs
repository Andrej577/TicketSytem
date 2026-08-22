using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TicketSystem.Shared.Authentication;

namespace TicketSystem.Client.Authentication;

public sealed class TicketSystemAuthenticationClient(HttpClient httpClient)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken)
            ?? throw new JsonException("The API returned an empty login response.");
    }
}
