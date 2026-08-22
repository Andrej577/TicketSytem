using System.Text.Json;
using TicketSystem.Client.Authentication;
using TicketSystem.Shared.Authentication;

namespace TicketSystem.Maui.Authentication;

public sealed class MauiAuthenticationSession : IAuthenticationSession
{
    private const string StorageKey = "TicketSystem.LoginResponse";

    public async Task<LoginResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var serialized = await SecureStorage.Default.GetAsync(StorageKey);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LoginResponse>(serialized);
        }
        catch (JsonException)
        {
            SecureStorage.Default.Remove(StorageKey);
            return null;
        }
    }

    public Task SetAsync(LoginResponse loginResponse, CancellationToken cancellationToken = default)
    {
        return SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(loginResponse));
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        SecureStorage.Default.Remove(StorageKey);
        return Task.CompletedTask;
    }
}
