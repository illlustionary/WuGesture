using Microsoft.Win32;

namespace WuGesture.App;

/// <summary>
/// Windows "app mode" preference, used when the configured theme is "system". Shared by the main window
/// chrome and the application picker so both resolve the same theme.
/// </summary>
internal static class SystemTheme
{
    public static bool PrefersDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }
}
