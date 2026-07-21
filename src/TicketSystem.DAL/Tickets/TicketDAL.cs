using Dapper;
using Npgsql;
using TicketSystem.Shared;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.POCO;

namespace TicketSystem.DAL.Tickets;

public sealed class TicketDAL
{
    private const string AppUserTable = "AppUser";
    private const string TicketPriorityTable = "TicketPriority";
    private const string TicketTable = "Ticket";

    private readonly NpgsqlDataSource dataSource;

    public TicketDAL(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<TicketPriorityDTO>> GetPrioritiesAsync(CancellationToken cancellationToken)
    {
        var sql = $"SELECT * FROM \"{TicketPriorityTable}\" ORDER BY \"Impact\";";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<TicketPriorityDTO>(command)).ToList();
    }

    public async Task<IReadOnlyList<TicketPOCO>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Ticket".*,
                "Customer"."Email" AS "CustomerEmail",
                "Operator"."Email" AS "OperatorEmail",
                "TicketPriority"."DisplayName" AS "PriorityDisplayName",
                "TicketPriority"."Impact" AS "PriorityImpact"
            FROM "{TicketTable}" AS "Ticket"
            INNER JOIN "{TicketPriorityTable}" AS "TicketPriority" ON "TicketPriority"."Id" = "Ticket"."PriorityId"
            INNER JOIN "{AppUserTable}" AS "Customer" ON "Customer"."Id" = "Ticket"."CustomerId"
            LEFT JOIN "{AppUserTable}" AS "Operator" ON "Operator"."Id" = "Ticket"."OperatorId"
            WHERE "Ticket"."IsDeleted" = false
            ORDER BY "Ticket"."CreatedAt" DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<TicketPOCO>(command)).ToList();
    }

    public async Task<TicketPOCO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Ticket".*,
                "Customer"."Email" AS "CustomerEmail",
                "Operator"."Email" AS "OperatorEmail",
                "TicketPriority"."DisplayName" AS "PriorityDisplayName",
                "TicketPriority"."Impact" AS "PriorityImpact"
            FROM "{TicketTable}" AS "Ticket"
            INNER JOIN "{TicketPriorityTable}" AS "TicketPriority" ON "TicketPriority"."Id" = "Ticket"."PriorityId"
            INNER JOIN "{AppUserTable}" AS "Customer" ON "Customer"."Id" = "Ticket"."CustomerId"
            LEFT JOIN "{AppUserTable}" AS "Operator" ON "Operator"."Id" = "Ticket"."OperatorId"
            WHERE "Ticket"."Id" = @Id AND "Ticket"."IsDeleted" = false;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<TicketPOCO>(command);
    }

    public async Task<TicketPOCO> CreateAsync(Guid customerId, Guid? operatorId, string title, string content, short statusId, short priorityId, DateTimeOffset? closedAt, CancellationToken cancellationToken)
    {
        var sql = $"""
            WITH "CreatedTicket" AS (
                INSERT INTO "{TicketTable}" (
                    "CustomerId",
                    "OperatorId",
                    "Title",
                    "Content",
                    "StatusId",
                    "PriorityId",
                    "ClosedAt",
                    "UpdatedByUserId"
                )
                VALUES (
                    @CustomerId,
                    @OperatorId,
                    @Title,
                    @Content,
                    @StatusId,
                    @PriorityId,
                    @ClosedAt,
                    @UpdatedByUserId
                )
                RETURNING *
            )
            SELECT
                "CreatedTicket".*,
                "Customer"."Email" AS "CustomerEmail",
                "Operator"."Email" AS "OperatorEmail",
                "TicketPriority"."DisplayName" AS "PriorityDisplayName",
                "TicketPriority"."Impact" AS "PriorityImpact"
            FROM "CreatedTicket"
            INNER JOIN "{TicketPriorityTable}" AS "TicketPriority" ON "TicketPriority"."Id" = "CreatedTicket"."PriorityId"
            INNER JOIN "{AppUserTable}" AS "Customer" ON "Customer"."Id" = "CreatedTicket"."CustomerId"
            LEFT JOIN "{AppUserTable}" AS "Operator" ON "Operator"."Id" = "CreatedTicket"."OperatorId";
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { CustomerId = customerId, OperatorId = operatorId, Title = title, Content = content, StatusId = statusId, PriorityId = priorityId, ClosedAt = closedAt, UpdatedByUserId = SystemUserIds.AdministratorId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<TicketPOCO>(command);
    }

    public async Task<bool> UpdateAsync(Guid id, Guid customerId, Guid? operatorId, string title, string content, short statusId, short priorityId, DateTimeOffset? closedAt, CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE "{TicketTable}"
            SET
                "CustomerId" = @CustomerId,
                "OperatorId" = @OperatorId,
                "Title" = @Title,
                "Content" = @Content,
                "StatusId" = @StatusId,
                "PriorityId" = @PriorityId,
                "ClosedAt" = @ClosedAt,
                "UpdatedAt" = now(),
                "UpdatedByUserId" = @UpdatedByUserId
            WHERE "Id" = @Id AND "IsDeleted" = false;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { Id = id, CustomerId = customerId, OperatorId = operatorId, Title = title, Content = content, StatusId = statusId, PriorityId = priorityId, ClosedAt = closedAt, UpdatedByUserId = SystemUserIds.AdministratorId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command) > 0;
    }

    public async Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"""UPDATE "{TicketTable}" SET "IsDeleted" = true, "UpdatedAt" = now(), "UpdatedByUserId" = @UpdatedByUserId WHERE "Id" = @Id AND "IsDeleted" = false;""";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { Id = id, UpdatedByUserId = SystemUserIds.AdministratorId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }
}
