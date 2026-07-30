namespace TicketSystem.Shared.DTO;

public sealed class DashboardSummaryDTO
{
    public int OpenTicketCount { get; set; }

    public int TicketsCreatedInRange { get; set; }

    public double? AvgResolutionHours { get; set; }

    public int ActiveChatSessionCount { get; set; }

    public int UrgentOpenTicketCount { get; set; }
}
