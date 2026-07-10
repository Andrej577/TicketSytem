namespace TicketSystem.Api.Features.UpdateDatabase;

public sealed record DatabaseMigration(int Version, string Sql);
