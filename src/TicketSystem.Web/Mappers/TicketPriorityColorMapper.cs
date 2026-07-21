using MudBlazor;

namespace TicketSystem.Web.Mappers;

public static class TicketPriorityColorMapper
{
    public static Color GetColor(short impact)
    {
        return impact switch
        {
            1 => Color.Default,
            2 => Color.Info,
            3 => Color.Warning,
            4 => Color.Error,
            _ => Color.Default
        };
    }
}
