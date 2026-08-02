using Dapper;
using Npgsql;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;

namespace TicketSystem.DAL.Dashboard;

public sealed class DashboardDAL
{
    private const string AppUserTable = "AppUser";
    private const string ChatSessionTable = "ChatSession";
    private const string KnowledgeTable = "Knowledge";
    private const string MessageTable = "Message";
    private const string TicketTable = "Ticket";
    private const string TicketPriorityTable = "TicketPriority";
    private const string TicketStatusTable = "TicketStatus";
    private const string TicketStatusHistoryTable = "TicketStatusHistory";

    private const short OpenStatusId = 1;
    private const short InProgressStatusId = 2;
    private const short UrgentPriorityId = 4;
    private const short ActiveChatSessionStatusId = 1;
    private const int OperatorUserTypeId = (int)AppUserType.Operator;
    private const int AdministratorUserTypeId = (int)AppUserType.Administrator;

    private readonly NpgsqlDataSource dataSource;

    public DashboardDAL(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<DashboardSummaryDTO> GetSummaryAsync(DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                (SELECT COUNT(*) FROM "{TicketTable}" WHERE "IsDeleted" = false AND "StatusId" IN (@OpenStatusId, @InProgressStatusId)) AS "OpenTicketCount",
                (SELECT COUNT(*) FROM "{TicketTable}" WHERE "IsDeleted" = false AND "CreatedAt" >= @FromDate AND "CreatedAt" <= @ToDate) AS "TicketsCreatedInRange",
                (SELECT AVG(EXTRACT(EPOCH FROM ("ClosedAt" - "CreatedAt")) / 3600.0) FROM "{TicketTable}" WHERE "IsDeleted" = false AND "ClosedAt" IS NOT NULL AND "ClosedAt" >= @FromDate AND "ClosedAt" <= @ToDate) AS "AvgResolutionHours",
                (SELECT COUNT(*) FROM "{ChatSessionTable}" WHERE "StatusId" = @ActiveChatSessionStatusId) AS "ActiveChatSessionCount",
                (SELECT COUNT(*) FROM "{TicketTable}" WHERE "IsDeleted" = false AND "StatusId" IN (@OpenStatusId, @InProgressStatusId) AND "PriorityId" = @UrgentPriorityId) AS "UrgentOpenTicketCount";
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { OpenStatusId, InProgressStatusId, UrgentPriorityId, ActiveChatSessionStatusId, FromDate = fromDate, ToDate = toDate };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<DashboardSummaryDTO>(command);
    }

    public async Task<IReadOnlyList<DashboardStatusCountDTO>> GetTicketsByStatusAsync(DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "TicketStatus"."Id" AS "StatusId",
                "TicketStatus"."Code" AS "StatusCode",
                "TicketStatus"."Name" AS "StatusName",
                COUNT("Ticket"."Id") AS "Count"
            FROM "{TicketStatusTable}" AS "TicketStatus"
            LEFT JOIN "{TicketTable}" AS "Ticket" ON "Ticket"."StatusId" = "TicketStatus"."Id"
                AND "Ticket"."IsDeleted" = false
                AND "Ticket"."CreatedAt" >= @FromDate AND "Ticket"."CreatedAt" <= @ToDate
            GROUP BY "TicketStatus"."Id", "TicketStatus"."Code", "TicketStatus"."Name"
            ORDER BY "TicketStatus"."Id";
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { FromDate = fromDate, ToDate = toDate }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<DashboardStatusCountDTO>(command)).ToList();
    }

    public async Task<IReadOnlyList<DashboardPriorityCountDTO>> GetTicketsByPriorityAsync(DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "TicketPriority"."DisplayName" AS "PriorityName",
                "TicketPriority"."Impact" AS "Impact",
                COUNT("Ticket"."Id") AS "Count"
            FROM "{TicketPriorityTable}" AS "TicketPriority"
            LEFT JOIN "{TicketTable}" AS "Ticket" ON "Ticket"."PriorityId" = "TicketPriority"."Id"
                AND "Ticket"."IsDeleted" = false
                AND "Ticket"."CreatedAt" >= @FromDate AND "Ticket"."CreatedAt" <= @ToDate
            GROUP BY "TicketPriority"."Id", "TicketPriority"."DisplayName", "TicketPriority"."Impact"
            ORDER BY "TicketPriority"."Impact";
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { FromDate = fromDate, ToDate = toDate }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<DashboardPriorityCountDTO>(command)).ToList();
    }

    public async Task<IReadOnlyList<DashboardFirstResponsePointDTO>> GetFirstResponseTrendAsync(DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken)
    {
        var sql = $"""
            WITH "FirstOperatorResponse" AS (
                SELECT
                    "Ticket"."Id" AS "TicketId",
                    "Ticket"."CreatedAt" AS "TicketCreatedAt",
                    MIN("Message"."SentAt") AS "FirstResponseAt"
                FROM "{TicketTable}" AS "Ticket"
                INNER JOIN "{MessageTable}" AS "Message" ON "Message"."ChatSessionId" = "Ticket"."ChatSessionId"
                INNER JOIN "{AppUserTable}" AS "Sender" ON "Sender"."Id" = "Message"."SenderId"
                WHERE "Ticket"."IsDeleted" = false
                    AND "Ticket"."ChatSessionId" IS NOT NULL
                    AND "Sender"."UserTypeId" IN (@OperatorUserTypeId, @AdministratorUserTypeId)
                    AND "Ticket"."CreatedAt" >= @FromDate AND "Ticket"."CreatedAt" <= @ToDate
                GROUP BY "Ticket"."Id", "Ticket"."CreatedAt"
            )
            SELECT
                CAST("TicketCreatedAt" AS date) AS "Date",
                AVG(EXTRACT(EPOCH FROM ("FirstResponseAt" - "TicketCreatedAt")) / 60.0) AS "AvgResponseMinutes"
            FROM "FirstOperatorResponse"
            GROUP BY CAST("TicketCreatedAt" AS date)
            ORDER BY CAST("TicketCreatedAt" AS date);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { OperatorUserTypeId, AdministratorUserTypeId, FromDate = fromDate, ToDate = toDate };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<DashboardFirstResponsePointDTO>(command)).ToList();
    }

    public async Task<IReadOnlyList<DashboardActivityItemDTO>> GetRecentActivityAsync(int limit, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT * FROM (
                SELECT
                    'TicketCreated' AS "EventType",
                    "Ticket"."CreatedAt" AS "OccurredAt",
                    "Ticket"."Title" AS "Title",
                    "Customer"."FirstName" || ' ' || "Customer"."LastName" AS "ActorName",
                    NULL AS "StatusName"
                FROM "{TicketTable}" AS "Ticket"
                INNER JOIN "{AppUserTable}" AS "Customer" ON "Customer"."Id" = "Ticket"."CustomerId"
                WHERE "Ticket"."IsDeleted" = false

                UNION ALL

                SELECT
                    'TicketStatusChanged' AS "EventType",
                    "StatusHistory"."ChangedAt" AS "OccurredAt",
                    "Ticket"."Title" AS "Title",
                    "Actor"."FirstName" || ' ' || "Actor"."LastName" AS "ActorName",
                    "NewStatus"."Name" AS "StatusName"
                FROM "{TicketStatusHistoryTable}" AS "StatusHistory"
                INNER JOIN "{TicketTable}" AS "Ticket" ON "Ticket"."Id" = "StatusHistory"."TicketId"
                INNER JOIN "{AppUserTable}" AS "Actor" ON "Actor"."Id" = "StatusHistory"."ChangedByUserId"
                INNER JOIN "{TicketStatusTable}" AS "NewStatus" ON "NewStatus"."Id" = "StatusHistory"."NewStatusId"
                WHERE "Ticket"."IsDeleted" = false

                UNION ALL

                SELECT
                    'UserCreated' AS "EventType",
                    "AppUser"."CreatedAt" AS "OccurredAt",
                    "AppUser"."FirstName" || ' ' || "AppUser"."LastName" AS "Title",
                    "AppUser"."FirstName" || ' ' || "AppUser"."LastName" AS "ActorName",
                    NULL AS "StatusName"
                FROM "{AppUserTable}" AS "AppUser"

                UNION ALL

                SELECT
                    'KnowledgePublished' AS "EventType",
                    "Knowledge"."PublishedAt" AS "OccurredAt",
                    "Knowledge"."Title" AS "Title",
                    "Author"."FirstName" || ' ' || "Author"."LastName" AS "ActorName",
                    NULL AS "StatusName"
                FROM "{KnowledgeTable}" AS "Knowledge"
                INNER JOIN "{AppUserTable}" AS "Author" ON "Author"."Id" = "Knowledge"."AuthorId"
                WHERE "Knowledge"."PublishedAt" IS NOT NULL
            ) AS "Activity"
            ORDER BY "OccurredAt" DESC
            LIMIT @Limit;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Limit = limit }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<DashboardActivityItemDTO>(command)).ToList();
    }
}
