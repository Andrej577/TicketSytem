using TicketSystem.Shared.DTO;

namespace TicketSystem.Shared.POCO;

public sealed class TicketPOCO : TicketDTO
{
    public string CustomerEmail { get; set; } = string.Empty;

    public string? OperatorEmail { get; set; }

    public string CustomerFirstName { get; set; } = string.Empty;

    public string CustomerLastName { get; set; } = string.Empty;

    public string? OperatorFirstName { get; set; }

    public string? OperatorLastName { get; set; }

    public string PriorityDisplayName { get; set; } = string.Empty;

    public short PriorityImpact { get; set; }

    public string CustomerName => $"{CustomerFirstName} {CustomerLastName}".Trim();

    public string? OperatorName => string.IsNullOrEmpty(OperatorFirstName) ? null : $"{OperatorFirstName} {OperatorLastName}".Trim();

    public TicketPOCO Copy()
    {
        return (TicketPOCO)MemberwiseClone();
    }
}
