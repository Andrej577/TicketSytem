namespace TicketSystem.Shared.DTO;

public sealed class DashboardActivityItemDTO
{
    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ActorName { get; set; }

    public string? StatusName { get; set; }
}
