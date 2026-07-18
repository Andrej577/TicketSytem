using Dapper;
using Npgsql;
using TicketSystem.Shared;
using TicketSystem.Shared.DTO;

namespace TicketSystem.DAL.Tickets;

public sealed class TicketDAL
{
    private const string TicketTable = "Ticket";

    private readonly NpgsqlDataSource dataSource;

    public TicketDAL(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<TicketDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Id",
                "TicketNumber",
                "ChatSessionId",
                "CustomerId",
                "OperatorId",
                "Title",
                "Content",
                "StatusId",
                "PriorityId",
                "CreatedAt",
                "UpdatedAt",
                "ClosedAt",
                "IsDeleted",
                "UpdatedByUserId"
            FROM "{TicketTable}"
            WHERE "IsDeleted" = false
            ORDER BY "CreatedAt" DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<TicketDTO>(command)).ToList();
    }

    public async Task<TicketDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Id",
                "TicketNumber",
                "ChatSessionId",
                "CustomerId",
                "OperatorId",
                "Title",
                "Content",
                "StatusId",
                "PriorityId",
                "CreatedAt",
                "UpdatedAt",
                "ClosedAt",
                "IsDeleted",
                "UpdatedByUserId"
            FROM "{TicketTable}"
            WHERE "Id" = @Id AND "IsDeleted" = false;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<TicketDTO>(command);
    }

    public async Task<TicketDTO> CreateAsync(Guid customerId, Guid? operatorId, string title, string content, short statusId, short priorityId, DateTimeOffset? closedAt, CancellationToken cancellationToken)
    {
        var sql = $"""
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
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { CustomerId = customerId, OperatorId = operatorId, Title = title, Content = content, StatusId = statusId, PriorityId = priorityId, ClosedAt = closedAt, UpdatedByUserId = SystemUserIds.AdministratorId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<TicketDTO>(command);
    }

    public async Task<TicketDTO?> UpdateAsync(Guid id, Guid customerId, Guid? operatorId, string title, string content, short statusId, short priorityId, DateTimeOffset? closedAt, CancellationToken cancellationToken)
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
            WHERE "Id" = @Id AND "IsDeleted" = false
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { Id = id, CustomerId = customerId, OperatorId = operatorId, Title = title, Content = content, StatusId = statusId, PriorityId = priorityId, ClosedAt = closedAt, UpdatedByUserId = SystemUserIds.AdministratorId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<TicketDTO>(command);
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
