using System.Security.Claims;
using Npgsql;
using TicketSystem.DAL.Tickets;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;
using TicketSystem.Shared.POCO;

namespace TicketSystem.Api.Features.Tickets;

public static class TicketEndpoints
{
    private const short OpenStatusId = 1;
    private const short NormalPriorityId = 2;

    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Customer), nameof(AppUserType.Operator), nameof(AppUserType.Administrator)));

        group.MapGet("/", GetTickets).WithName("GetTickets").Produces<IReadOnlyList<TicketPOCO>>(StatusCodes.Status200OK);
        group.MapGet("/{id:guid}", GetTicket).WithName("GetTicket").Produces<TicketPOCO>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateTicket).RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Customer), nameof(AppUserType.Administrator))).WithName("CreateTicket").Produces<TicketPOCO>(StatusCodes.Status201Created).Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateTicket).RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Operator), nameof(AppUserType.Administrator))).WithName("UpdateTicket").Produces<bool>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", DeleteTicket).RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Administrator))).WithName("DeleteTicket").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/ticket-priorities", GetTicketPriorities).RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Customer), nameof(AppUserType.Operator), nameof(AppUserType.Administrator))).WithTags("Ticket priorities").WithName("GetTicketPriorities").Produces<IReadOnlyList<TicketPriorityDTO>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<IResult> GetTicketPriorities(TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        return Results.Ok(await ticketDAL.GetPrioritiesAsync(cancellationToken));
    }

    private static async Task<IResult> GetTickets(ClaimsPrincipal user, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(user);
        var customerOnly = user.IsInRole(nameof(AppUserType.Customer));
        return Results.Ok(await ticketDAL.GetAllAsync(currentUserId, customerOnly, cancellationToken));
    }

    private static async Task<IResult> GetTicket(Guid id, ClaimsPrincipal user, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(user);
        var customerOnly = user.IsInRole(nameof(AppUserType.Customer));
        var ticket = await ticketDAL.GetByIdAsync(id, currentUserId, customerOnly, cancellationToken);
        return ticket is null ? Results.NotFound() : Results.Ok(ticket);
    }

    private static async Task<IResult> CreateTicket(CreateTicketRequest request, ClaimsPrincipal user, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId(user);
            var isCustomer = user.IsInRole(nameof(AppUserType.Customer));
            var ticket = await ticketDAL.CreateAsync(
                isCustomer ? currentUserId : request.CustomerId ?? Guid.Empty,
                isCustomer ? null : request.OperatorId,
                request.Title,
                request.Content,
                isCustomer ? OpenStatusId : request.StatusId ?? 0,
                isCustomer ? NormalPriorityId : request.PriorityId ?? 0,
                isCustomer ? null : request.ClosedAt,
                currentUserId,
                cancellationToken);
            return Results.Created($"/api/tickets/{ticket.Id}", ticket);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The ticket references an invalid customer, operator, status, or priority." });
        }
    }

    private static async Task<IResult> UpdateTicket(Guid id, UpdateTicketRequest request, ClaimsPrincipal user, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetCurrentUserId(user);
            var wasUpdated = user.IsInRole(nameof(AppUserType.Administrator))
                ? await ticketDAL.UpdateAsync(id, request.CustomerId, request.OperatorId, request.Title, request.Content, request.StatusId, request.PriorityId, request.ClosedAt, currentUserId, cancellationToken)
                : await ticketDAL.UpdateAssignedAsync(id, currentUserId, request.Title, request.Content, request.StatusId, request.PriorityId, request.ClosedAt, cancellationToken);
            return wasUpdated ? Results.Ok(true) : Results.NotFound();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The ticket references an invalid customer, operator, status, or priority." });
        }
    }

    private static async Task<IResult> DeleteTicket(Guid id, ClaimsPrincipal user, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        var deletedRows = await ticketDAL.DeleteAsync(id, GetCurrentUserId(user), cancellationToken);
        return deletedRows == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
