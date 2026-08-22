using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Configuration;
using MudBlazor.Services;
using TicketSystem.Client;
using TicketSystem.Client.Authentication;
using TicketSystem.Maui.Authentication;
using TicketSystem.SharedUI.Services;

namespace TicketSystem.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        var apiBaseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:8081" : "http://localhost:8081";
        var realtimeBaseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:8082" : "http://localhost:8082";
        builder.Configuration["Api:BaseUrl"] = apiBaseUrl;
        builder.Configuration["Realtime:BaseUrl"] = realtimeBaseUrl;
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        });
        builder.Services.AddSingleton<IAuthenticationSession, MauiAuthenticationSession>();
        builder.Services.AddSingleton<TicketSystemAuthenticationStateProvider>();
        builder.Services.AddSingleton<AuthenticationStateProvider>(serviceProvider => serviceProvider.GetRequiredService<TicketSystemAuthenticationStateProvider>());
        builder.Services.AddSingleton<IAccessTokenProvider, SessionAccessTokenProvider>();
        builder.Services.AddSingleton<IApiUnauthorizedHandler, MauiApiUnauthorizedHandler>();
        builder.Services.AddSingleton<IMediaFileDownloadService, MauiMediaFileDownloadService>();
        builder.Services.AddSingleton<TicketSystemAuthenticationClient>();
        builder.Services.AddSingleton<TicketSystemApiClient>();
        builder.Services.AddSingleton<ThemeState>();
        builder.Services.AddSingleton<LoadingState>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
