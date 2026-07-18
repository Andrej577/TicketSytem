using Npgsql;
using TicketSystem.DAL.Tickets;
using TicketSystem.Shared.DTO;

namespace TicketSystem.Api.Features.Tickets;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tickets").WithTags("Tickets");

        group.MapGet("/", GetTickets).WithName("GetTickets").Produces<IReadOnlyList<TicketDTO>>(StatusCodes.Status200OK);
        group.MapGet("/{id:guid}", GetTicket).WithName("GetTicket").Produces<TicketDTO>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateTicket).WithName("CreateTicket").Produces<TicketDTO>(StatusCodes.Status201Created).Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateTicket).WithName("UpdateTicket").Produces<TicketDTO>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", DeleteTicket).WithName("DeleteTicket").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetTickets(TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        return Results.Ok(await ticketDAL.GetAllAsync(cancellationToken));
    }

    private static async Task<IResult> GetTicket(Guid id, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        var ticket = await ticketDAL.GetByIdAsync(id, cancellationToken);
        return ticket is null ? Results.NotFound() : Results.Ok(ticket);
    }

    private static async Task<IResult> CreateTicket(CreateTicketRequest request, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await ticketDAL.CreateAsync(request.CustomerId, request.OperatorId, request.Title, request.Content, request.StatusId, request.PriorityId, request.ClosedAt, cancellationToken);
            return Results.Created($"/api/tickets/{ticket.Id}", ticket);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The ticket references an invalid customer, operator, status, or priority." });
        }
    }

    private static async Task<IResult> UpdateTicket(Guid id, UpdateTicketRequest request, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await ticketDAL.UpdateAsync(id, request.CustomerId, request.OperatorId, request.Title, request.Content, request.StatusId, request.PriorityId, request.ClosedAt, cancellationToken);
            return ticket is null ? Results.NotFound() : Results.Ok(ticket);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The ticket references an invalid customer, operator, status, or priority." });
        }
    }

    private static async Task<IResult> DeleteTicket(Guid id, TicketDAL ticketDAL, CancellationToken cancellationToken)
    {
        var deletedRows = await ticketDAL.DeleteAsync(id, cancellationToken);
        return deletedRows == 0 ? Results.NotFound() : Results.NoContent();
    }
}
