using TicketSystem.Shared.DTO;

namespace TicketSystem.Shared.POCO;

public sealed class KnowledgePOCO : KnowledgeDTO
{
    public string? CategoryName { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string AuthorEmail { get; set; } = string.Empty;
}
