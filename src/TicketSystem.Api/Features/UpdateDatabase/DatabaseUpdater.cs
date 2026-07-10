using Npgsql;

namespace TicketSystem.Api.Features.UpdateDatabase;

public sealed class DatabaseUpdater
{
    private const long AdvisoryLockKey = 820250703;
    private readonly string connectionString;
    private readonly IReadOnlyList<DatabaseMigration> migrations;
    private readonly ILogger<DatabaseUpdater> logger;

    public DatabaseUpdater(
        string connectionString,
        IReadOnlyList<DatabaseMigration> migrations,
        ILogger<DatabaseUpdater> logger)
    {
        this.connectionString = connectionString;
        this.migrations = migrations.OrderBy(migration => migration.Version).ToArray();
        this.logger = logger;
    }

    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureVersionTableAsync(connection, cancellationToken);

        foreach (var migration in migrations)
        {
            await ApplyMigrationAsync(connection, migration, cancellationToken);
        }
    }

    private static async Task EnsureVersionTableAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS database_version (
                version integer PRIMARY KEY,
                applied_at timestamp with time zone NOT NULL DEFAULT now()
            );

            ALTER TABLE database_version
            DROP COLUMN IF EXISTS name;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ApplyMigrationAsync(
        NpgsqlConnection connection,
        DatabaseMigration migration,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await LockVersionTableAsync(connection, transaction, cancellationToken);

        if (await IsMigrationAppliedAsync(connection, transaction, migration.Version, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        logger.LogInformation(
            "Applying database migration {Version}",
            migration.Version);

        await using (var migrationCommand = new NpgsqlCommand(migration.Sql, connection, transaction))
        {
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var versionCommand = new NpgsqlCommand(
            "INSERT INTO database_version (version) VALUES (@version);",
            connection,
            transaction))
        {
            versionCommand.Parameters.AddWithValue("version", migration.Version);
            await versionCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Applied database migration {Version}",
            migration.Version);
    }

    private static async Task LockVersionTableAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(@lockKey);",
            connection,
            transaction);

        command.Parameters.AddWithValue("lockKey", AdvisoryLockKey);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> IsMigrationAppliedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int version,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM database_version WHERE version = @version);",
            connection,
            transaction);

        command.Parameters.AddWithValue("version", version);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is true;
    }
}
