using Dapper;
using Npgsql;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;
using TicketSystem.Shared.POCO;

namespace TicketSystem.DAL.Knowledge;

public sealed class KnowledgeDAL
{
    private const string AppUserTable = "AppUser";
    private const string KnowledgeCategoryTable = "KnowledgeCategory";
    private const string KnowledgeStatusTable = "KnowledgeStatus";
    private const string KnowledgeTable = "Knowledge";
    private const short DraftStatusId = (short)KnowledgeStatusType.Draft;
    private const short PublishedStatusId = (short)KnowledgeStatusType.Published;

    private readonly NpgsqlDataSource dataSource;

    public KnowledgeDAL(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<KnowledgePOCO>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Knowledge".*,
                "KnowledgeCategory"."Name" AS "CategoryName",
                "KnowledgeStatus"."Name" AS "StatusName",
                "KnowledgeStatus"."Code" AS "StatusCode",
                "Author"."Email" AS "AuthorEmail"
            FROM "{KnowledgeTable}" AS "Knowledge"
            LEFT JOIN "{KnowledgeCategoryTable}" AS "KnowledgeCategory" ON "KnowledgeCategory"."Id" = "Knowledge"."CategoryId"
            INNER JOIN "{KnowledgeStatusTable}" AS "KnowledgeStatus" ON "KnowledgeStatus"."Id" = "Knowledge"."StatusId"
            INNER JOIN "{AppUserTable}" AS "Author" ON "Author"."Id" = "Knowledge"."AuthorId"
            ORDER BY "Knowledge"."UpdatedAt" DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<KnowledgePOCO>(command)).ToList();
    }

    public async Task<KnowledgePOCO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Knowledge".*,
                "KnowledgeCategory"."Name" AS "CategoryName",
                "KnowledgeStatus"."Name" AS "StatusName",
                "KnowledgeStatus"."Code" AS "StatusCode",
                "Author"."Email" AS "AuthorEmail"
            FROM "{KnowledgeTable}" AS "Knowledge"
            LEFT JOIN "{KnowledgeCategoryTable}" AS "KnowledgeCategory" ON "KnowledgeCategory"."Id" = "Knowledge"."CategoryId"
            INNER JOIN "{KnowledgeStatusTable}" AS "KnowledgeStatus" ON "KnowledgeStatus"."Id" = "Knowledge"."StatusId"
            INNER JOIN "{AppUserTable}" AS "Author" ON "Author"."Id" = "Knowledge"."AuthorId"
            WHERE "Knowledge"."Id" = @Id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<KnowledgePOCO>(command);
    }

    public async Task<IReadOnlyList<KnowledgeCategoryDTO>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var sql = $"""SELECT * FROM "{KnowledgeCategoryTable}" ORDER BY "Name";""";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<KnowledgeCategoryDTO>(command)).ToList();
    }

    public async Task<IReadOnlyList<KnowledgeStatusDTO>> GetStatusesAsync(CancellationToken cancellationToken)
    {
        var sql = $"""SELECT * FROM "{KnowledgeStatusTable}" ORDER BY "Id";""";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<KnowledgeStatusDTO>(command)).ToList();
    }

    public async Task<KnowledgePOCO> CreateAsync(string title, string content, Guid? categoryId, short statusId, Guid authorId, CancellationToken cancellationToken)
    {
        var sql = $"""
            WITH "CreatedKnowledge" AS (
                INSERT INTO "{KnowledgeTable}" (
                    "Title",
                    "Content",
                    "CategoryId",
                    "StatusId",
                    "AuthorId",
                    "PublishedAt"
                )
                VALUES (
                    @Title,
                    @Content,
                    @CategoryId,
                    @StatusId,
                    @AuthorId,
                    CASE WHEN @StatusId = @PublishedStatusId THEN now() ELSE NULL END
                )
                RETURNING *
            )
            SELECT
                "CreatedKnowledge".*,
                "KnowledgeCategory"."Name" AS "CategoryName",
                "KnowledgeStatus"."Name" AS "StatusName",
                "KnowledgeStatus"."Code" AS "StatusCode",
                "Author"."Email" AS "AuthorEmail"
            FROM "CreatedKnowledge"
            LEFT JOIN "{KnowledgeCategoryTable}" AS "KnowledgeCategory" ON "KnowledgeCategory"."Id" = "CreatedKnowledge"."CategoryId"
            INNER JOIN "{KnowledgeStatusTable}" AS "KnowledgeStatus" ON "KnowledgeStatus"."Id" = "CreatedKnowledge"."StatusId"
            INNER JOIN "{AppUserTable}" AS "Author" ON "Author"."Id" = "CreatedKnowledge"."AuthorId";
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { Title = title, Content = content, CategoryId = categoryId, StatusId = statusId, AuthorId = authorId, PublishedStatusId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<KnowledgePOCO>(command);
    }

    public async Task<bool> UpdateAsync(Guid id, string title, string content, Guid? categoryId, short statusId, CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE "{KnowledgeTable}"
            SET
                "Title" = @Title,
                "Content" = @Content,
                "CategoryId" = @CategoryId,
                "StatusId" = @StatusId,
                "UpdatedAt" = now(),
                "PublishedAt" = CASE
                    WHEN @StatusId = @PublishedStatusId THEN COALESCE("PublishedAt", now())
                    WHEN @StatusId = @DraftStatusId THEN NULL
                    ELSE "PublishedAt"
                END
            WHERE "Id" = @Id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { Id = id, Title = title, Content = content, CategoryId = categoryId, StatusId = statusId, PublishedStatusId, DraftStatusId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command) > 0;
    }

    public async Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"""DELETE FROM "{KnowledgeTable}" WHERE "Id" = @Id;""";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }
}
