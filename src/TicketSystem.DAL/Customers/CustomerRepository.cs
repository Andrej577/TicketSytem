using Dapper;
using Npgsql;
using TicketSystem.DAL.AppUsers;
using TicketSystem.Shared.DTO;

namespace TicketSystem.DAL.Customers;

public sealed class CustomerRepository
{
    private readonly NpgsqlDataSource dataSource;

    public CustomerRepository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IReadOnlyList<CustomerDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                customer.id AS "Id",
                customer.app_user_id AS "AppUserId",
                app_user.email AS "Email",
                customer.created_at AS "CreatedAt",
                customer.updated_at AS "UpdatedAt"
            FROM customer
            INNER JOIN app_user ON app_user.id = customer.app_user_id
            ORDER BY customer.created_at DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<CustomerDTO>(command)).ToList();
    }

    public async Task<CustomerDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await FindByIdAsync(connection, null, id, cancellationToken);
    }

    public async Task<CustomerDTO> CreateAsync(string email, string passwordHash, int userTypeId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var appUser = await AppUserRepository.CreateAsync(connection, transaction, email, passwordHash, userTypeId, cancellationToken);
        var customer = await InsertAsync(connection, transaction, appUser, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return customer;
    }

    public async Task<CustomerDTO?> UpdateAsync(Guid id, string email, string? passwordHash, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var appUserId = await FindAppUserIdAsync(connection, transaction, id, cancellationToken);

        if (appUserId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await AppUserRepository.UpdateAsync(connection, transaction, appUserId.Value, email, passwordHash, cancellationToken);
        await TouchAsync(connection, transaction, id, cancellationToken);
        var customer = await FindByIdAsync(connection, transaction, id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return customer;
    }

    public async Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM app_user
            WHERE id = (SELECT app_user_id FROM customer WHERE customer.id = @Id);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    private static async Task<CustomerDTO> InsertAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, AppUserDTO appUser, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO customer (app_user_id)
            VALUES (@AppUserId)
            RETURNING
                id AS "Id",
                app_user_id AS "AppUserId",
                CAST(@Email AS text) AS "Email",
                created_at AS "CreatedAt",
                updated_at AS "UpdatedAt";
            """;

        var parameters = new { AppUserId = appUser.Id, Email = appUser.Email };
        var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<CustomerDTO>(command);
    }

    private static async Task<CustomerDTO?> FindByIdAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                customer.id AS "Id",
                customer.app_user_id AS "AppUserId",
                app_user.email AS "Email",
                customer.created_at AS "CreatedAt",
                customer.updated_at AS "UpdatedAt"
            FROM customer
            INNER JOIN app_user ON app_user.id = customer.app_user_id
            WHERE customer.id = @Id;
            """;

        var command = new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CustomerDTO>(command);
    }

    private static async Task<Guid?> FindAppUserIdAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT app_user_id FROM customer WHERE id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    private static async Task TouchAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE customer SET updated_at = now() WHERE id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
