using MudBlazor;
using TicketSystem.Shared.Enums;

namespace TicketSystem.SharedUI.Mappers;

public static class KnowledgeStatusColorMapper
{
    public static Color GetColor(short statusId)
    {
        return (KnowledgeStatusType)statusId switch
        {
            KnowledgeStatusType.Published => Color.Success,
            KnowledgeStatusType.Archived => Color.Warning,
            _ => Color.Default
        };
    }
}
