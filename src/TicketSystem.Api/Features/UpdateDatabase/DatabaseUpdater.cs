using Npgsql;
using TicketSystem.DAL.Database;

namespace TicketSystem.Api.Features.UpdateDatabase;

public sealed class DatabaseUpdater
{
    private readonly string connectionString;
    private readonly IReadOnlyList<DatabaseMigration> migrations;
    private readonly ILogger<DatabaseUpdater> logger;

    public DatabaseUpdater(string connectionString, IReadOnlyList<DatabaseMigration> migrations, ILogger<DatabaseUpdater> logger)
    {
        this.connectionString = connectionString;
        this.migrations = migrations.OrderBy(migration => migration.Version).ToArray();
        this.logger = logger;
    }

    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await DatabaseMigrationExecutor.EnsureVersionTableAsync(connection, cancellationToken);

        foreach (var migration in migrations)
        {
            if (!await DatabaseMigrationExecutor.ApplyIfPendingAsync(connection, migration, cancellationToken))
            {
                continue;
            }

            logger.LogInformation("Applied database migration {Version}", migration.Version);
        }
    }
}
