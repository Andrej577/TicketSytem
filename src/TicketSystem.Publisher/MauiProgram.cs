using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.LifecycleEvents;
using MudBlazor.Services;
using TicketSystem.Publisher.Services;

#if WINDOWS
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace TicketSystem.Publisher;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddSingleton(new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        });
        builder.Services.AddSingleton<ProcessRunner>();
        builder.Services.AddSingleton<AndroidLauncher>();
        builder.Services.AddSingleton<LocalPublishService>();

#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windows => windows.OnWindowCreated(window =>
            {
                var displayArea = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Nearest);
                var workArea = displayArea.WorkArea;
                var windowSize = window.AppWindow.Size;
                var centeredPosition = new PointInt32(
                    workArea.X + (workArea.Width - windowSize.Width) / 2,
                    workArea.Y + (workArea.Height - windowSize.Height) / 2);

                window.AppWindow.Move(centeredPosition);
            }));
        });
#endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
