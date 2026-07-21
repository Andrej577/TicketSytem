using MudBlazor;

namespace TicketSystem.Web.Models;

public static class TicketStatusOptions
{
    public const short ClosedId = 4;

    public static IReadOnlyList<TicketStatusOption> All { get; } =
    [
        new(1, "Open", Color.Info, "mud-border-info"),
        new(2, "In progress", Color.Warning, "mud-border-warning"),
        new(3, "Resolved", Color.Success, "mud-border-success"),
        new(4, "Closed", Color.Error, "mud-border-error")
    ];
}

public sealed record TicketStatusOption(short Id, string Name, Color Color, string DropBorderClass);
