# Database Update Schema

This folder contains a small database update mechanism for the TicketSystem API.
It is not an Entity Framework migration setup. It is a lightweight PostgreSQL
schema updater that runs SQL migrations when the API starts.

## Purpose

The updater keeps the database schema aligned with the application code.
Each schema change is represented as a numbered `DatabaseMigration`. When the
API starts, the updater checks which migration versions were already applied and
runs only the missing ones.

This gives the project a repeatable way to create or update the database without
manually running `schema.sql` every time.

## Startup Flow

The flow starts in `Program.cs`:

```csharp
builder.Services.AddDatabaseUpdater();
```

`AddDatabaseUpdater()` registers `DatabaseUpdateHostedService` as a hosted
service. ASP.NET Core starts hosted services during application startup, before
the API begins handling requests.

The hosted service:

1. Reads `ConnectionStrings:DefaultConnection` from configuration.
2. Skips the update if the connection string is missing.
3. Creates a `DatabaseUpdater`.
4. Passes all registered migrations from `DatabaseMigrations.All`.
5. Runs `UpdateAsync()`.

## Version Tracking Table

Before applying migrations, the updater ensures this table exists:

```sql
CREATE TABLE IF NOT EXISTS database_version (
    version integer PRIMARY KEY,
    applied_at timestamp with time zone NOT NULL DEFAULT now()
);
```

This table is the source of truth for executed migrations.

- `version` is the unique migration number.
- `applied_at` records when the migration was applied.

If a migration version already exists in `database_version`, the updater skips
that migration.

## Migration Ordering

`DatabaseUpdater` sorts migrations by `Version` before running them:

```csharp
this.migrations = migrations.OrderBy(migration => migration.Version).ToArray();
```

This means migrations should always use increasing version numbers:

- `1` for the initial schema.
- `2` for the next schema change.
- `3` for the change after that.

Do not reuse an existing version number for a different change.

## Transaction Behavior

Each migration is applied inside its own database transaction.

For every migration, the updater:

1. Begins a transaction.
2. Acquires a PostgreSQL advisory transaction lock.
3. Checks whether the migration version is already applied.
4. Executes the migration SQL if it is missing.
5. Inserts a row into `database_version`.
6. Commits the transaction.

If the SQL fails, the transaction is not committed, and the migration version is
not recorded. The next startup can try to apply it again after the problem is
fixed.

## Advisory Lock

The updater uses this PostgreSQL lock:

```sql
SELECT pg_advisory_xact_lock(@lockKey);
```

The lock protects the update process when multiple API instances start at the
same time. Only one instance can apply a migration at a time. Other instances
wait for the transaction lock, then re-check `database_version` before deciding
whether the migration still needs to run.

The current lock key is defined in `DatabaseUpdater`:

```csharp
private const long AdvisoryLockKey = 820250703;
```

## Current Migration

The first migration is registered in `DatabaseMigrations.All`:

```csharp
new(1, CreateInitialTables())
```

The current database schema version is also stored explicitly in
`DatabaseMigrations.DatabaseVersion`.

It creates:

- `pgcrypto` extension for UUID generation.
- `app_user`
- `chat_session`
- `ticket`
- `message`
- `message_read`
- indexes for common lookup paths.

The SQL is built from small private methods so each table definition is easier
to read and change.

## Adding a New Migration

To add a database change:

1. Add a new `DatabaseMigration` entry in `DatabaseMigrations.All`.
2. Use the next available version number.
3. Update `DatabaseMigrations.DatabaseVersion` to the newest version number.
4. Put the SQL in a new private method.

Example:

```csharp
public static IReadOnlyList<DatabaseMigration> All { get; } =
[
    new(1, CreateInitialTables()),
    new(2, AddTicketDueDate())
];

private static string AddTicketDueDate()
{
    return """
        ALTER TABLE ticket
        ADD COLUMN IF NOT EXISTS due_at timestamp with time zone;
        """;
}
```

Prefer idempotent SQL where PostgreSQL supports it, such as:

- `CREATE TABLE IF NOT EXISTS`
- `CREATE INDEX IF NOT EXISTS`
- `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`

The version table already prevents a migration from running twice, but
idempotent SQL makes local development and recovery safer.

## Configuration

The updater expects this connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ticket_system;Username=postgres;Password=postgres"
  }
}
```

If `DefaultConnection` is empty or missing, startup continues and the updater
logs a warning:

```text
Database update was skipped because ConnectionStrings:DefaultConnection is not configured.
```

## Operational Notes

- The updater runs at API startup.
- It currently targets PostgreSQL through `Npgsql`.
- It does not roll back already committed older migrations if a later migration
  fails.
- It does not support automatic down migrations.
- Migration SQL should be reviewed carefully because it runs with the configured
  database user's permissions.
- Existing migrations should not be edited after they have been applied to a
  shared database. Add a new migration instead.
