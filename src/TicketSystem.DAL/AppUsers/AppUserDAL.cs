using Dapper;
using Npgsql;
using TicketSystem.Shared;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.POCO;

namespace TicketSystem.DAL.AppUsers;

public sealed class AppUserDAL
{
    private const string AppUserTable = "AppUser";

    private readonly NpgsqlDataSource dataSource;

    public AppUserDAL(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<AppUserDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Id",
                "Email",
                "UserTypeId",
                "CreatedAt",
                "UpdatedAt",
                "UpdatedByUserId"
            FROM "{AppUserTable}"
            ORDER BY "CreatedAt" DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<AppUserDTO>(command)).ToList();
    }

    public async Task<IReadOnlyList<AppUserPOCO>> GetUsersWithEmailEdited(CancellationToken cancellationToken)
    {
        var sql = $"WITH \"Users\" AS (SELECT \"Id\", \"Email\", \"UserTypeId\", \"CreatedAt\", \"UpdatedAt\", \"UpdatedByUserId\" FROM \"{AppUserTable}\") SELECT \"Users\".\"Id\", \"Users\".\"Email\", \"Users\".\"UserTypeId\", \"Users\".\"CreatedAt\", \"Users\".\"UpdatedAt\", \"Users\".\"UpdatedByUserId\", \"UpdatedByUser\".\"Email\" AS \"UpdatedByUserEmail\" FROM \"Users\" INNER JOIN \"{AppUserTable}\" AS \"UpdatedByUser\" ON \"UpdatedByUser\".\"Id\" = \"Users\".\"UpdatedByUserId\" ORDER BY \"Users\".\"CreatedAt\" DESC;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<AppUserPOCO>(command)).ToList();
    }

    public async Task<AppUserDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Id",
                "Email",
                "UserTypeId",
                "CreatedAt",
                "UpdatedAt",
                "UpdatedByUserId"
            FROM "{AppUserTable}"
            WHERE "Id" = @Id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AppUserDTO>(command);
    }

    public async Task<AppUserDTO> CreateAsync(string email, string passwordHash, int userTypeId, CancellationToken cancellationToken)
    {
        var sql = $"""
            INSERT INTO "{AppUserTable}" ("Email", "PasswordHash", "UserTypeId", "UpdatedByUserId")
            VALUES (@Email, @PasswordHash, @UserTypeId, @UpdatedByUserId)
            RETURNING
                "Id",
                "Email",
                "UserTypeId",
                "CreatedAt",
                "UpdatedAt",
                "UpdatedByUserId";
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { Email = email, PasswordHash = passwordHash, UserTypeId = userTypeId, UpdatedByUserId = SystemUserIds.AdministratorId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<AppUserDTO>(command);
    }

    public async Task<AppUserDTO?> UpdateAsync(Guid id, string email, string? passwordHash, CancellationToken cancellationToken)
    {
        var updatePassword = passwordHash is not null;
        var sql = updatePassword
            ? $"""
                UPDATE "{AppUserTable}"
                SET "Email" = @Email, "PasswordHash" = @PasswordHash, "UpdatedAt" = now(), "UpdatedByUserId" = @UpdatedByUserId
                WHERE "Id" = @Id
                RETURNING
                    "Id",
                    "Email",
                    "UserTypeId",
                    "CreatedAt",
                    "UpdatedAt",
                    "UpdatedByUserId";
                """
            : $"""
                UPDATE "{AppUserTable}"
                SET "Email" = @Email, "UpdatedAt" = now(), "UpdatedByUserId" = @UpdatedByUserId
                WHERE "Id" = @Id
                RETURNING
                    "Id",
                    "Email",
                    "UserTypeId",
                    "CreatedAt",
                    "UpdatedAt",
                    "UpdatedByUserId";
                """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);
        parameters.Add("Email", email);
        parameters.Add("UpdatedByUserId", SystemUserIds.AdministratorId);

        if (updatePassword)
        {
            parameters.Add("PasswordHash", passwordHash);
        }

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AppUserDTO>(command);
    }

    public async Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"DELETE FROM \"{AppUserTable}\" WHERE \"Id\" = @Id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }
}
