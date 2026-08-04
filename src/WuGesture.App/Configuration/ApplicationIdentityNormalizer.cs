namespace WuGesture.App.GestureEngine;

internal static class ApplicationIdentityNormalizer
{
    public static string NormalizeProcessName(string? value)
    {
        var trimmed = (value ?? "").Trim();
        var name = Path.GetFileNameWithoutExtension(trimmed);
        return string.IsNullOrWhiteSpace(name) ? trimmed : name;
    }
}
