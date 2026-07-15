using Npgsql;
using TicketSystem.Api.Features.AppUsers;
using TicketSystem.DAL.Customers;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Api.Features.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", GetCustomers).WithName("GetCustomers").Produces<IReadOnlyList<CustomerDTO>>(StatusCodes.Status200OK);
        group.MapGet("/{id:guid}", GetCustomer).WithName("GetCustomer").Produces<CustomerDTO>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateCustomer).WithName("CreateCustomer").Produces<CustomerDTO>(StatusCodes.Status201Created).ProducesValidationProblem().Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateCustomer).WithName("UpdateCustomer").Produces<CustomerDTO>(StatusCodes.Status200OK).ProducesValidationProblem().Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", DeleteCustomer).WithName("DeleteCustomer").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetCustomers(CustomerRepository repository, CancellationToken cancellationToken)
    {
        return Results.Ok(await repository.GetAllAsync(cancellationToken));
    }

    private static async Task<IResult> GetCustomer(Guid id, CustomerRepository repository, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(id, cancellationToken);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    }

    private static async Task<IResult> CreateCustomer(CreateCustomerRequest request, CustomerRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var email = AppUserInputValidator.NormalizeEmail(request.Email!);
            var passwordHash = PasswordHasher.Hash(request.Password);
            var customer = await repository.CreateAsync(email, passwordHash, (int)UserType.Customer, cancellationToken);
            return Results.Created($"/api/customers/{customer.Id}", customer);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Results.Conflict(new { message = "A customer with this email already exists." });
        }
    }

    private static async Task<IResult> UpdateCustomer(Guid id, UpdateCustomerRequest request, CustomerRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var email = AppUserInputValidator.NormalizeEmail(request.Email!);
            var passwordHash = request.Password is null ? null : PasswordHasher.Hash(request.Password);
            var customer = await repository.UpdateAsync(id, email, passwordHash, cancellationToken);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Results.Conflict(new { message = "A customer with this email already exists." });
        }
    }

    private static async Task<IResult> DeleteCustomer(Guid id, CustomerRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var deletedRows = await repository.DeleteAsync(id, cancellationToken);
            return deletedRows == 0 ? Results.NotFound() : Results.NoContent();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The customer is still referenced by protected records." });
        }
    }
}
