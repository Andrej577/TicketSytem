namespace TicketSystem.Client.Authentication;

public sealed class SessionAccessTokenProvider(IAuthenticationSession session) : IAccessTokenProvider
{
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var loginResponse = await session.GetAsync(cancellationToken);
        return loginResponse is not null && loginResponse.ExpiresAt > DateTimeOffset.UtcNow
            ? loginResponse.AccessToken
            : null;
    }
}
