using MudBlazor;

namespace TicketSystem.Web.Themes;

public static class TicketSystemTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#16d6aa",
            PrimaryContrastText = "#05251f",
            Secondary = "#36c8dd",
            Background = "#071210",
            Surface = "#0d1d1b",
            AppbarBackground = "#101722",
            AppbarText = "#e8f2f0",
            TextPrimary = "#e8f2f0",
            TextSecondary = "#819a96",
            Divider = "#203b37",
            DividerLight = "#18312d",
            ActionDefault = "#91aaa6",
            ActionDisabled = "#536763",
            ActionDisabledBackground = "#122824",
            Success = "#16d6aa",
            Error = "#ff6b6b",
            Warning = "#f4bf62",
            Info = "#36c8dd"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        }
    };
}
