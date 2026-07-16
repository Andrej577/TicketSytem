# Database Update Schema

This folder contains the lightweight PostgreSQL schema updater used by the
TicketSystem API. It is not an Entity Framework migration setup. The updater
runs numbered SQL migrations when the API starts.

`DatabaseMigrations` is the runtime source of truth. The root `schema.sql` file
mirrors the complete `UpgradeTo1` baseline so the schema can also be inspected
or created manually.

## Startup Flow

The flow starts in `Program.cs`:

```csharp
builder.Services.AddDatabaseUpdater();
```

`AddDatabaseUpdater()` registers `DatabaseUpdateHostedService`. During API
startup, the hosted service:

1. Reads `ConnectionStrings:DefaultConnection`.
2. Skips the update when the connection string is missing.
3. Creates a `DatabaseUpdater`.
4. Passes the migrations from `DatabaseMigrations.All`.
5. Applies pending migrations in ascending version order.

## Version Tracking

Before applying migrations, the updater ensures this table exists:

```sql
CREATE TABLE IF NOT EXISTS "DatabaseVersion" (
    "Version" integer NOT NULL,
    "AppliedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_DatabaseVersion" PRIMARY KEY ("Version")
);
```

`Version` is the unique migration number, and `AppliedAt` records when it was
applied. A version already present in `DatabaseVersion` is skipped.

## Migration Naming and Ordering

Migration methods are named after their target version:

```csharp
public static int DatabaseVersion { get; } = 1;

public static IReadOnlyList<DatabaseMigration> All { get; } =
[
    new(1, UpgradeTo1())
];
```

The next schema change must use version `2` and method `UpgradeTo2()`. Never
reuse an applied version number for different SQL. Once a baseline is used by a
shared database, preserve it and add a new migration instead of changing it.

## UpgradeTo1 Baseline

`UpgradeTo1()` contains the complete initial schema in this order:

1. Enables `pgcrypto` and creates the quoted `TicketNumberSequence` sequence.
2. Creates all tables without inline foreign keys.
3. Adds every foreign key as an explicitly named constraint.
4. Assigns sequence ownership and creates indexes.
5. Inserts lookup values.
6. Inserts the initial administrator last.

Application-owned PostgreSQL tables, columns, indexes, and the sequence use
quoted PascalCase names. Constraints use an uppercase type prefix followed by
one underscore and a PascalCase name, such as `PK_AppUser` and
`FK_TicketCustomerIdAppUser`. SQL queries must continue to quote identifiers
with their exact casing.

The baseline has no separate Customer table. `ChatSession.CustomerId` and
`Ticket.CustomerId` reference `AppUser.Id`. Customer identity is represented by
`AppUser.UserTypeId`.

`AppUser.UpdatedByUserId` is required and has a self-referencing foreign key
with `ON DELETE RESTRICT`. The initial administrator uses the same fixed ID for
both `Id` and `UpdatedByUserId`:

```text
2d6781ce-863a-4ca4-83c3-c4d521f8e23d
```

## Adding a Migration

To add the next database change:

1. Increase `DatabaseMigrations.DatabaseVersion`.
2. Add the next numbered entry to `DatabaseMigrations.All`.
3. Add a private method whose name matches the target version.
4. Put only the new schema delta in that method.

Example:

```csharp
public static int DatabaseVersion { get; } = 2;

public static IReadOnlyList<DatabaseMigration> All { get; } =
[
    new(1, UpgradeTo1()),
    new(2, UpgradeTo2())
];

private static string UpgradeTo2()
{
    return """
        ALTER TABLE "Ticket"
        ADD COLUMN "DueAt" timestamp with time zone;
        """;
}
```

## Transaction and Lock Behavior

Each migration runs in its own transaction. For every migration, the updater:

1. Begins a transaction.
2. Acquires the PostgreSQL advisory transaction lock.
3. Checks `DatabaseVersion` for the migration version.
4. Executes the SQL when the version is pending.
5. Inserts the applied version.
6. Commits the transaction.

If the SQL fails, the transaction is not committed and the version is not
recorded. The next startup can retry it after the problem is corrected.

The advisory lock key is defined in `DatabaseMigrationExecutor`. It prevents
multiple API instances from applying the same migration concurrently.

## Configuration

The updater expects this connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ticket_system;Username=postgres;Password=postgres"
  }
}
```

If `DefaultConnection` is missing, startup continues and the updater logs that
the database update was skipped.

## Reset Requirement

The previous five development migrations were squashed into `UpgradeTo1`, and
all database identifiers changed from unquoted snake_case to quoted PascalCase.
An existing database that recorded the previous versions is not compatible
with this reset baseline. Local development must recreate the database and its
version-tracking table instead of only restarting the API.

## Operational Notes

- The updater runs during API startup.
- PostgreSQL is accessed through Npgsql.
- The updater has no automatic down migrations.
- A later migration failure does not roll back earlier committed migrations.
- The configured database user must have permission to create the required
  extension and schema objects.
- Keep `schema.sql` synchronized with `UpgradeTo1` whenever the initial baseline
  is intentionally changed before it is shared.
