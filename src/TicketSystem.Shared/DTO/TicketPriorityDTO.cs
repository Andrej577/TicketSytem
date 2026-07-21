namespace TicketSystem.Shared.DTO;

public sealed class TicketPriorityDTO
{
    public short Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public short Impact { get; set; }
}
