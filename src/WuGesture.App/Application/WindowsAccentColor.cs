using Microsoft.Win32;

namespace WuGesture.App;

/// <summary>
/// Reads the Windows accent colour so the web theme can use it as a dynamic seed. The DWM registry value is
/// the stable fallback when the WinRT colour API is unavailable; the result is cached briefly because the
/// rules payload is rebuilt on every configuration load.
/// </summary>
internal static class WindowsAccentColor
{
    public const string Fallback = "#529EF1";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(2);
    private static readonly object Sync = new();
    private static string? cachedValue;
    private static DateTime cachedAtUtc;

    public static string Read()
    {
        lock (Sync)
        {
            var now = DateTime.UtcNow;
            if (cachedValue is not null && now - cachedAtUtc < CacheDuration)
            {
                return cachedValue;
            }

            cachedValue = ReadCore() ?? Fallback;
            cachedAtUtc = now;
            return cachedValue;
        }
    }

    private static string? ReadCore()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("ColorizationColor") is int value)
            {
                return $"#{value & 0x00FFFFFF:X6}";
            }
        }
        catch
        {
            // Registry access can be blocked by policy; the preset seed is a safe fallback.
        }

        return null;
    }
}
