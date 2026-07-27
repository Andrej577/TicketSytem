using TicketSystem.Shared.DTO;

namespace TicketSystem.Shared.POCO;

public sealed class MessagePOCO : MessageDTO
{
    public string SenderFirstName { get; set; } = string.Empty;

    public string SenderLastName { get; set; } = string.Empty;
}
