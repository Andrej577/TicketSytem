using Dapper;
using Npgsql;
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

    public async Task<AppUserLoginData?> GetForLoginAsync(string email, CancellationToken cancellationToken)
    {
        var sql = $"""SELECT "Id", "Email", "FirstName", "LastName", "PasswordHash", "UserTypeId" FROM "{AppUserTable}" WHERE "Email" = @Email;""";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AppUserLoginData>(command);
    }

    public async Task<IReadOnlyList<AppUserDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                "Id",
                "Email",
                "FirstName",
                "LastName",
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
        var sql = $"WITH \"Users\" AS (SELECT \"Id\", \"Email\", \"FirstName\", \"LastName\", \"UserTypeId\", \"CreatedAt\", \"UpdatedAt\", \"UpdatedByUserId\" FROM \"{AppUserTable}\") SELECT \"Users\".\"Id\", \"Users\".\"Email\", \"Users\".\"FirstName\", \"Users\".\"LastName\", \"Users\".\"UserTypeId\", \"Users\".\"CreatedAt\", \"Users\".\"UpdatedAt\", \"Users\".\"UpdatedByUserId\", \"UpdatedByUser\".\"Email\" AS \"UpdatedByUserEmail\" FROM \"Users\" INNER JOIN \"{AppUserTable}\" AS \"UpdatedByUser\" ON \"UpdatedByUser\".\"Id\" = \"Users\".\"UpdatedByUserId\" ORDER BY \"Users\".\"CreatedAt\" DESC;";

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
                "FirstName",
                "LastName",
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

    public async Task<AppUserDTO> CreateAsync(string email, string firstName, string lastName, string passwordHash, int userTypeId, Guid updatedByUserId, CancellationToken cancellationToken)
    {
        var sql = $"""
            INSERT INTO "{AppUserTable}" ("Email", "FirstName", "LastName", "PasswordHash", "UserTypeId", "UpdatedByUserId")
            VALUES (@Email, @FirstName, @LastName, @PasswordHash, @UserTypeId, @UpdatedByUserId)
            RETURNING
                "Id",
                "Email",
                "FirstName",
                "LastName",
                "UserTypeId",
                "CreatedAt",
                "UpdatedAt",
                "UpdatedByUserId";
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new { Email = email, FirstName = firstName, LastName = lastName, PasswordHash = passwordHash, UserTypeId = userTypeId, UpdatedByUserId = updatedByUserId };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<AppUserDTO>(command);
    }

    public async Task<AppUserDTO?> UpdateAsync(Guid id, string email, string firstName, string lastName, string? passwordHash, Guid updatedByUserId, CancellationToken cancellationToken)
    {
        var updatePassword = passwordHash is not null;
        var sql = updatePassword
            ? $"""
                UPDATE "{AppUserTable}"
                SET "Email" = @Email, "FirstName" = @FirstName, "LastName" = @LastName, "PasswordHash" = @PasswordHash, "UpdatedAt" = now(), "UpdatedByUserId" = @UpdatedByUserId
                WHERE "Id" = @Id
                RETURNING
                    "Id",
                    "Email",
                    "FirstName",
                    "LastName",
                    "UserTypeId",
                    "CreatedAt",
                    "UpdatedAt",
                    "UpdatedByUserId";
                """
            : $"""
                UPDATE "{AppUserTable}"
                SET "Email" = @Email, "FirstName" = @FirstName, "LastName" = @LastName, "UpdatedAt" = now(), "UpdatedByUserId" = @UpdatedByUserId
                WHERE "Id" = @Id
                RETURNING
                    "Id",
                    "Email",
                    "FirstName",
                    "LastName",
                    "UserTypeId",
                    "CreatedAt",
                    "UpdatedAt",
                    "UpdatedByUserId";
                """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);
        parameters.Add("Email", email);
        parameters.Add("FirstName", firstName);
        parameters.Add("LastName", lastName);
        parameters.Add("UpdatedByUserId", updatedByUserId);

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
