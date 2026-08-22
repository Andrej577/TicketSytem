using TicketSystem.Shared.Authentication;

namespace TicketSystem.Client.Authentication;

public interface IAuthenticationSession
{
    Task<LoginResponse?> GetAsync(CancellationToken cancellationToken = default);

    Task SetAsync(LoginResponse loginResponse, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
