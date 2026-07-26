using System.Security.Claims;
using Npgsql;
using TicketSystem.DAL.Knowledge;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;
using TicketSystem.Shared.POCO;

namespace TicketSystem.Api.Features.Knowledge;

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/knowledge")
            .WithTags("Knowledge")
            .RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Operator), nameof(AppUserType.Administrator)));

        group.MapGet("/", GetKnowledge).WithName("GetKnowledge").Produces<IReadOnlyList<KnowledgePOCO>>(StatusCodes.Status200OK);
        group.MapGet("/{id:guid}", GetKnowledgeById).WithName("GetKnowledgeById").Produces<KnowledgePOCO>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateKnowledge).WithName("CreateKnowledge").Produces<KnowledgePOCO>(StatusCodes.Status201Created).ProducesValidationProblem().Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateKnowledge).WithName("UpdateKnowledge").Produces<KnowledgePOCO>(StatusCodes.Status200OK).ProducesValidationProblem().Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", DeleteKnowledge).RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Administrator))).WithName("DeleteKnowledge").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/knowledge-categories", GetKnowledgeCategories)
            .RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Operator), nameof(AppUserType.Administrator)))
            .WithTags("Knowledge categories")
            .WithName("GetKnowledgeCategories")
            .Produces<IReadOnlyList<KnowledgeCategoryDTO>>(StatusCodes.Status200OK);

        endpoints.MapGet("/api/knowledge-statuses", GetKnowledgeStatuses)
            .RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Operator), nameof(AppUserType.Administrator)))
            .WithTags("Knowledge statuses")
            .WithName("GetKnowledgeStatuses")
            .Produces<IReadOnlyList<KnowledgeStatusDTO>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<IResult> GetKnowledge(KnowledgeDAL knowledgeDAL, CancellationToken cancellationToken)
    {
        return Results.Ok(await knowledgeDAL.GetAllAsync(cancellationToken));
    }

    private static async Task<IResult> GetKnowledgeById(Guid id, KnowledgeDAL knowledgeDAL, CancellationToken cancellationToken)
    {
        var knowledge = await knowledgeDAL.GetByIdAsync(id, cancellationToken);
        return knowledge is null ? Results.NotFound() : Results.Ok(knowledge);
    }

    private static async Task<IResult> GetKnowledgeCategories(KnowledgeDAL knowledgeDAL, CancellationToken cancellationToken)
    {
        return Results.Ok(await knowledgeDAL.GetCategoriesAsync(cancellationToken));
    }

    private static async Task<IResult> GetKnowledgeStatuses(KnowledgeDAL knowledgeDAL, CancellationToken cancellationToken)
    {
        return Results.Ok(await knowledgeDAL.GetStatusesAsync(cancellationToken));
    }

    private static async Task<IResult> CreateKnowledge(CreateKnowledgeRequest request, ClaimsPrincipal user, KnowledgeDAL knowledgeDAL, KnowledgeRealtimeNotifier knowledgeRealtimeNotifier, CancellationToken cancellationToken)
    {
        var validationProblem = ValidateKnowledge(request.Title, request.Content, request.StatusId);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        try
        {
            var knowledge = await knowledgeDAL.CreateAsync(request.Title.Trim(), request.Content.Trim(), request.CategoryId, request.StatusId, GetCurrentUserId(user), cancellationToken);
            await knowledgeRealtimeNotifier.NotifyChangedAsync();
            return Results.Created($"/api/knowledge/{knowledge.Id}", knowledge);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The knowledge article references an invalid category or status." });
        }
    }

    private static async Task<IResult> UpdateKnowledge(Guid id, UpdateKnowledgeRequest request, KnowledgeDAL knowledgeDAL, KnowledgeRealtimeNotifier knowledgeRealtimeNotifier, CancellationToken cancellationToken)
    {
        var validationProblem = ValidateKnowledge(request.Title, request.Content, request.StatusId);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        try
        {
            var wasUpdated = await knowledgeDAL.UpdateAsync(id, request.Title.Trim(), request.Content.Trim(), request.CategoryId, request.StatusId, cancellationToken);
            if (!wasUpdated)
            {
                return Results.NotFound();
            }

            await knowledgeRealtimeNotifier.NotifyChangedAsync();
            return Results.Ok(await knowledgeDAL.GetByIdAsync(id, cancellationToken));
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Results.Conflict(new { message = "The knowledge article references an invalid category or status." });
        }
    }

    private static async Task<IResult> DeleteKnowledge(Guid id, KnowledgeDAL knowledgeDAL, KnowledgeRealtimeNotifier knowledgeRealtimeNotifier, CancellationToken cancellationToken)
    {
        var deletedRows = await knowledgeDAL.DeleteAsync(id, cancellationToken);
        if (deletedRows == 0)
        {
            return Results.NotFound();
        }

        await knowledgeRealtimeNotifier.NotifyChangedAsync();
        return Results.NoContent();
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private static IResult? ValidateKnowledge(string title, string content, short statusId)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(title))
        {
            errors[nameof(title)] = ["Title is required."];
        }
        else if (title.Trim().Length > 200)
        {
            errors[nameof(title)] = ["Title cannot exceed 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            errors[nameof(content)] = ["Content is required."];
        }

        if (!Enum.IsDefined((KnowledgeStatusType)statusId))
        {
            errors[nameof(statusId)] = ["Status is invalid."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }
}
