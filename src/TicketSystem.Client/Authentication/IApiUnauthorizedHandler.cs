namespace TicketSystem.Client.Authentication;

public interface IApiUnauthorizedHandler
{
    Task HandleAsync(CancellationToken cancellationToken = default);
}
