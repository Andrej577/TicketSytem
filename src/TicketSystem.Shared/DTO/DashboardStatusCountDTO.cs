namespace TicketSystem.Shared.DTO;

public sealed class DashboardStatusCountDTO
{
    public short StatusId { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string StatusName { get; set; } = string.Empty;

    public int Count { get; set; }
}
