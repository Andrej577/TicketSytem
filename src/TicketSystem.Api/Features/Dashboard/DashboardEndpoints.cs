using TicketSystem.DAL.Dashboard;
using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    private const int RecentActivityLimit = 15;
    private const int DefaultRangeDays = 30;

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization(policy => policy.RequireRole(nameof(AppUserType.Administrator)));

        group.MapGet("/", GetDashboard).WithName("GetDashboard").Produces<DashboardDTO>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<IResult> GetDashboard(DateTimeOffset? fromDate, DateTimeOffset? toDate, DashboardDAL dashboardDAL, CancellationToken cancellationToken)
    {
        var effectiveToDate = toDate ?? DateTimeOffset.UtcNow;
        var effectiveFromDate = fromDate ?? effectiveToDate.AddDays(-DefaultRangeDays);

        var summaryTask = dashboardDAL.GetSummaryAsync(effectiveFromDate, effectiveToDate, cancellationToken);
        var ticketsByStatusTask = dashboardDAL.GetTicketsByStatusAsync(effectiveFromDate, effectiveToDate, cancellationToken);
        var ticketsByPriorityTask = dashboardDAL.GetTicketsByPriorityAsync(effectiveFromDate, effectiveToDate, cancellationToken);
        var firstResponseTrendTask = dashboardDAL.GetFirstResponseTrendAsync(effectiveFromDate, effectiveToDate, cancellationToken);
        var recentActivityTask = dashboardDAL.GetRecentActivityAsync(RecentActivityLimit, cancellationToken);

        await Task.WhenAll(summaryTask, ticketsByStatusTask, ticketsByPriorityTask, firstResponseTrendTask, recentActivityTask);

        var dashboard = new DashboardDTO
        {
            Summary = summaryTask.Result,
            TicketsByStatus = ticketsByStatusTask.Result,
            TicketsByPriority = ticketsByPriorityTask.Result,
            FirstResponseTrend = firstResponseTrendTask.Result,
            RecentActivity = recentActivityTask.Result,
        };

        return Results.Ok(dashboard);
    }
}
