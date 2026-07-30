namespace TicketSystem.Shared.DTO;

public sealed class DashboardPriorityCountDTO
{
    public string PriorityName { get; set; } = string.Empty;

    public short Impact { get; set; }

    public int Count { get; set; }
}
