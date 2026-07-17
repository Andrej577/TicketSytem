using Npgsql;
using TicketSystem.DAL.AppUsers;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Api.Features.AppUsers;

public static class AppUserEndpoints
{
    public static IEndpointRouteBuilder MapAppUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/app-users").WithTags("App users");

        group.MapGet("/", GetAppUsers).WithName("GetAppUsers").Produces<IReadOnlyList<AppUserDTO>>(StatusCodes.Status200OK);
        group.MapGet("/customers", GetCustomers).WithName("GetCustomers").Produces<IReadOnlyList<AppUserDTO>>(StatusCodes.Status200OK);
        group.MapGet("/{id:guid}", GetAppUser).WithName("GetAppUser").Produces<AppUserDTO>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAppUser).WithName("CreateAppUser").Produces<AppUserDTO>(StatusCodes.Status201Created).ProducesValidationProblem().Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateAppUser).WithName("UpdateAppUser").Produces<AppUserDTO>(StatusCodes.Status200OK).ProducesValidationProblem().Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", DeleteAppUser).WithName("DeleteAppUser").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetAppUsers(AppUserRepository repository, CancellationToken cancellationToken)
    {
        return Results.Ok(await repository.GetAllAsync(cancellationToken));
    }

    private static async Task<IResult> GetCustomers(AppUserRepository repository, CancellationToken cancellationToken)
    {
        return Results.Ok(await repository.GetByUserTypeAsync((int)UserType.Customer, cancellationToken));
    }

    private static async Task<IResult> GetAppUser(Guid id, AppUserRepository repository, CancellationToken cancellationToken)
    {
        var appUser = await repository.GetByIdAsync(id, cancellationToken);
        return appUser is null ? Results.NotFound() : Results.Ok(appUser);
    }

    private static async Task<IResult> CreateAppUser(CreateAppUserRequest request, AppUserRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var passwordHash = PasswordHasher.Hash(request.Password);
            var appUser = await repository.CreateAsync(request.Email!, passwordHash, request.UserTypeId, cancellationToken);
            return Results.Created($"/api/app-users/{appUser.Id}", appUser);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Results.Conflict(new { message = "An app user with this email already exists." });
        }
    }

    private static async Task<IResult> UpdateAppUser(Guid id, UpdateAppUserRequest request, AppUserRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var passwordHash = request.Password is null ? null : PasswordHasher.Hash(request.Password);
            var appUser = await repository.UpdateAsync(id, request.Email!, passwordHash, cancellationToken);
            return appUser is null ? Results.NotFound() : Results.Ok(appUser);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Results.Conflict(new { message = "An app user with this email already exists." });
        }
    }

    private static async Task<IResult> DeleteAppUser(Guid id, AppUserRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var deletedRows = await repository.DeleteAsync(id, cancellationToken);
            return deletedRows == 0 ? Results.NotFound() : Results.NoContent();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The app user is still referenced by other records." });
        }
    }
}
