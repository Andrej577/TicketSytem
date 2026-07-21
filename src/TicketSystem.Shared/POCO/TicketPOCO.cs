using TicketSystem.Shared.DTO;

namespace TicketSystem.Shared.POCO;

public sealed class TicketPOCO : TicketDTO
{
    public string CustomerEmail { get; set; } = string.Empty;

    public string? OperatorEmail { get; set; }

    public string PriorityDisplayName { get; set; } = string.Empty;

    public short PriorityImpact { get; set; }

    public TicketPOCO Copy()
    {
        return (TicketPOCO)MemberwiseClone();
    }
}
