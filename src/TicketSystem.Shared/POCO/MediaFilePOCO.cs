using TicketSystem.Shared.DTO;

namespace TicketSystem.Shared.POCO;

public sealed class MediaFilePOCO : MediaFileDTO
{
    public string UploaderFirstName { get; set; } = string.Empty;

    public string UploaderLastName { get; set; } = string.Empty;
}
