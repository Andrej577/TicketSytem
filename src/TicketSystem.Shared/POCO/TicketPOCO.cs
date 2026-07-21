using TicketSystem.Shared.DTO;

namespace TicketSystem.Shared.POCO;

public sealed class TicketPOCO : TicketDTO
{
    public string PriorityDisplayName { get; set; } = string.Empty;

    public short PriorityImpact { get; set; }
}
