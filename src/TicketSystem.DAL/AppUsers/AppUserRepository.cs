using Dapper;
using Npgsql;
using TicketSystem.Shared.DTO;

namespace TicketSystem.DAL.AppUsers;

public sealed class AppUserRepository
{
    private readonly NpgsqlDataSource dataSource;

    public AppUserRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<AppUserDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id AS "Id",
                email AS "Email",
                user_type_id AS "UserTypeId",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt"
            FROM app_user
            ORDER BY created_at DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<AppUserDTO>(command)).ToList();
    }

    public async Task<AppUserDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id AS "Id",
                email AS "Email",
                user_type_id AS "UserTypeId",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt"
            FROM app_user
            WHERE id = @Id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AppUserDTO>(command);
    }

    public async Task<AppUserDTO> CreateAsync(string email, string passwordHash, int userTypeId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await CreateAsync(connection, null, email, passwordHash, userTypeId, cancellationToken);
    }

    public async Task<AppUserDTO?> UpdateAsync(Guid id, string email, string? passwordHash, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await UpdateAsync(connection, null, id, email, passwordHash, cancellationToken);
    }

    public async Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM app_user WHERE id = @Id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    internal static async Task<AppUserDTO> CreateAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, string email, string passwordHash, int userTypeId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO app_user (email, password_hash, user_type_id)
            VALUES (@Email, @PasswordHash, @UserTypeId)
            RETURNING
                id AS "Id",
                email AS "Email",
                user_type_id AS "UserTypeId",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt";
            """;

        var parameters = new { Email = email, PasswordHash = passwordHash, UserTypeId = userTypeId };
        var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<AppUserDTO>(command);
    }

    internal static async Task<AppUserDTO?> UpdateAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid id, string email, string? passwordHash, CancellationToken cancellationToken)
    {
        var updatePassword = passwordHash is not null;
        var sql = updatePassword
            ? """
                UPDATE app_user
                SET email = @Email, password_hash = @PasswordHash, updated_at = now()
                WHERE id = @Id
                RETURNING
                    id AS "Id",
                    email AS "Email",
                    user_type_id AS "UserTypeId",
                    created_at AS "CreatedAt",
                    updated_at AS "UpdatedAt";
                """
            : """
                UPDATE app_user
                SET email = @Email, updated_at = now()
                WHERE id = @Id
                RETURNING
                    id AS "Id",
                    email AS "Email",
                    user_type_id AS "UserTypeId",
                    created_at AS "CreatedAt",
                    updated_at AS "UpdatedAt";
                """;

        var parameters = new DynamicParameters();
        parameters.Add("Id", id);
        parameters.Add("Email", email);

        if (updatePassword)
        {
            parameters.Add("PasswordHash", passwordHash);
        }

        var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AppUserDTO>(command);
    }
}
