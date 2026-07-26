using System.Security.Claims;
using Npgsql;
using TicketSystem.DAL.AppUsers;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;
using TicketSystem.Shared.POCO;

namespace TicketSystem.Api.Features.AppUsers;

public static class AppUserEndpoints
{
    public static IEndpointRouteBuilder MapAppUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/app-users")
            .RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Administrator)))
            .WithTags("App users");

        group.MapGet("/", GetAppUsers).WithName("GetAppUsers").Produces<IReadOnlyList<AppUserDTO>>(StatusCodes.Status200OK);
        group.MapGet("/users-with-email-edited", GetUsersWithEmailEdited).WithName("GetUsersWithEmailEdited").Produces<IReadOnlyList<AppUserPOCO>>(StatusCodes.Status200OK);
        group.MapGet("/{id:guid}", GetAppUser).WithName("GetAppUser").Produces<AppUserDTO>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAppUser).WithName("CreateAppUser").Produces<AppUserDTO>(StatusCodes.Status201Created).ProducesValidationProblem().Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateAppUser).WithName("UpdateAppUser").Produces<AppUserDTO>(StatusCodes.Status200OK).ProducesValidationProblem().Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", DeleteAppUser).WithName("DeleteAppUser").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetAppUsers(AppUserDAL appUserDAL, CancellationToken cancellationToken)
    {
        return Results.Ok(await appUserDAL.GetAllAsync(cancellationToken));
    }

    private static async Task<IResult> GetUsersWithEmailEdited(AppUserDAL appUserDAL, CancellationToken cancellationToken)
    {
        return Results.Ok(await appUserDAL.GetUsersWithEmailEdited(cancellationToken));
    }

    private static async Task<IResult> GetAppUser(Guid id, AppUserDAL appUserDAL, CancellationToken cancellationToken)
    {
        var appUser = await appUserDAL.GetByIdAsync(id, cancellationToken);
        return appUser is null ? Results.NotFound() : Results.Ok(appUser);
    }

    private static async Task<IResult> CreateAppUser(CreateAppUserRequest request, ClaimsPrincipal user, AppUserDAL appUserDAL, CancellationToken cancellationToken)
    {
        try
        {
            var passwordHash = PasswordHasher.Hash(request.Password);
            var appUser = await appUserDAL.CreateAsync(request.Email!, passwordHash, request.UserTypeId, GetCurrentUserId(user), cancellationToken);
            return Results.Created($"/api/app-users/{appUser.Id}", appUser);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Results.Conflict(new { message = "An app user with this email already exists." });
        }
    }

    private static async Task<IResult> UpdateAppUser(Guid id, UpdateAppUserRequest request, ClaimsPrincipal user, AppUserDAL appUserDAL, CancellationToken cancellationToken)
    {
        try
        {
            var passwordHash = request.Password is null ? null : PasswordHasher.Hash(request.Password);
            var appUser = await appUserDAL.UpdateAsync(id, request.Email!, passwordHash, GetCurrentUserId(user), cancellationToken);
            return appUser is null ? Results.NotFound() : Results.Ok(appUser);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Results.Conflict(new { message = "An app user with this email already exists." });
        }
    }

    private static async Task<IResult> DeleteAppUser(Guid id, AppUserDAL appUserDAL, CancellationToken cancellationToken)
    {
        try
        {
            var deletedRows = await appUserDAL.DeleteAsync(id, cancellationToken);
            return deletedRows == 0 ? Results.NotFound() : Results.NoContent();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The app user is still referenced by other records." });
        }
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
