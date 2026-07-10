namespace TicketSystem.Api.Features.UpdateDatabase;

public static class UpdateDatabaseExtensions
{
    public static IServiceCollection AddDatabaseUpdater(this IServiceCollection services)
    {
        services.AddHostedService<DatabaseUpdateHostedService>();

        return services;
    }
}
