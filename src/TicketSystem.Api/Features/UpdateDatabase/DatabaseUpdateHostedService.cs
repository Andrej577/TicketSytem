using TicketSystem.DAL.Database;

namespace TicketSystem.Api.Features.UpdateDatabase;

public sealed class DatabaseUpdateHostedService : IHostedService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<DatabaseUpdater> updaterLogger;
    private readonly ILogger<DatabaseUpdateHostedService> logger;

    public DatabaseUpdateHostedService(
        IConfiguration configuration,
        ILogger<DatabaseUpdater> updaterLogger,
        ILogger<DatabaseUpdateHostedService> logger)
    {
        this.configuration = configuration;
        this.updaterLogger = updaterLogger;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "Database update was skipped because ConnectionStrings:DefaultConnection is not configured.");
            return;
        }

        var updater = new DatabaseUpdater(
            connectionString,
            DatabaseMigrations.All,
            updaterLogger);

        await updater.UpdateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
