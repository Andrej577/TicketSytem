using Dapper;
using Npgsql;

namespace TicketSystem.DAL.Database;

public static class DatabaseMigrationExecutor
{
    private const long AdvisoryLockKey = 820250703;
    private const string DatabaseVersionTable = "DatabaseVersion";

    public static async Task EnsureVersionTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = $"""
            CREATE TABLE IF NOT EXISTS "{DatabaseVersionTable}" (
                "Version" integer NOT NULL,
                "AppliedAt" timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT "PK_DatabaseVersion" PRIMARY KEY ("Version")
            );
            """;

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public static async Task<bool> ApplyIfPendingAsync(NpgsqlConnection connection, DatabaseMigration migration, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await LockVersionTableAsync(connection, transaction, cancellationToken);

        if (await IsAppliedAsync(connection, transaction, migration.Version, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        var migrationCommand = new CommandDefinition(migration.Sql, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(migrationCommand);

        var insertVersionSql = $"INSERT INTO \"{DatabaseVersionTable}\" (\"Version\") VALUES (@Version);";
        var versionCommand = new CommandDefinition(insertVersionSql, new { migration.Version }, transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(versionCommand);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static async Task LockVersionTableAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = "SELECT pg_advisory_xact_lock(@LockKey);";
        var command = new CommandDefinition(sql, new { LockKey = AdvisoryLockKey }, transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    private static async Task<bool> IsAppliedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int version, CancellationToken cancellationToken)
    {
        var sql = $"SELECT EXISTS (SELECT 1 FROM \"{DatabaseVersionTable}\" WHERE \"Version\" = @Version);";
        var command = new CommandDefinition(sql, new { Version = version }, transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<bool>(command);
    }
}
