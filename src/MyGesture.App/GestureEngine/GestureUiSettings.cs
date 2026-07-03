using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureUiSettings
{
    public MouseTrailUiSettings MouseTrail { get; set; } = new();

    public GestureHintUiSettings GestureHint { get; set; } = new();
}

public sealed class MouseTrailUiSettings
{
    public string InactiveColor { get; set; } = "#FFAAAAAA";

    public string ActiveColor { get; set; } = "#FF87CEEB";

    public float InactiveThickness { get; set; } = 3f;

    public float ActiveThickness { get; set; } = 3f;

    public float Thickness { get; set; } = 3f;

    public int InactiveOpacity { get; set; } = 74;

    public int ActiveOpacity { get; set; } = 100;
}

public sealed class GestureHintUiSettings
{
    public string FontFamily { get; set; } = "Segoe UI Semibold";

    public float FontSize { get; set; } = 22f;

    public string TextColor { get; set; } = "#FFFFFF";

    public string BackgroundColor { get; set; } = "#12181F";

    public int BackgroundOpacity { get; set; } = 90;

    public int Width { get; set; } = 540;

    public bool AutoWidth { get; set; }

    public int Height { get; set; } = 120;

    public int BottomOffset { get; set; } = 140;
}

internal static class GestureColorParser
{
    public static Color Parse(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim();

        if (normalized.StartsWith('#'))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length == 6 && int.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            return Color.FromArgb(unchecked((int)0xFF000000) | rgb);
        }

        if (normalized.Length == 8 &&
            uint.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, null, out var argb))
        {
            return Color.FromArgb(unchecked((int)argb));
        }

        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }
}
