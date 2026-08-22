using Microsoft.JSInterop;

namespace TicketSystem.SharedUI.Services;

public sealed class ThemeState
{
    private const string StorageKey = "TicketSystem.IsDarkMode";

    private bool isInitialized;

    public bool IsDarkMode { get; private set; } = true;

    public event Action? Changed;

    public async Task InitializeAsync(IJSRuntime jsRuntime)
    {
        if (isInitialized)
        {
            return;
        }

        var storedValue = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (bool.TryParse(storedValue, out var isDarkMode))
        {
            IsDarkMode = isDarkMode;
        }

        isInitialized = true;
        Changed?.Invoke();
    }

    public async Task SetDarkModeAsync(bool isDarkMode, IJSRuntime jsRuntime)
    {
        IsDarkMode = isDarkMode;
        Changed?.Invoke();
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, isDarkMode.ToString());
    }
}
