namespace TicketSystem.DAL.Database;

public sealed record DatabaseMigration(int Version, string Sql);
