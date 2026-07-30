namespace TicketSystem.Shared.DTO;

public sealed class DashboardDTO
{
    public DashboardSummaryDTO Summary { get; set; } = new();

    public IReadOnlyList<DashboardStatusCountDTO> TicketsByStatus { get; set; } = [];

    public IReadOnlyList<DashboardPriorityCountDTO> TicketsByPriority { get; set; } = [];

    public IReadOnlyList<DashboardFirstResponsePointDTO> FirstResponseTrend { get; set; } = [];

    public IReadOnlyList<DashboardActivityItemDTO> RecentActivity { get; set; } = [];
}
