using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureUiSettings
{
    public MouseTrailUiSettings MouseTrail { get; set; } = new();

    public GestureHintUiSettings GestureHint { get; set; } = new();

    public LevelOsdUiSettings LevelOsd { get; set; } = new();

    public GestureSensitivityUiSettings GestureSensitivity { get; set; } = new();

    public AppBehaviorUiSettings AppBehavior { get; set; } = new();

    public WebDavUiSettings WebDav { get; set; } = new();
}

public sealed class AppBehaviorUiSettings
{
    public bool LaunchAtStartup { get; set; }

    public bool ShowConfigWindowOnLaunch { get; set; } = true;

    public bool RunAsAdministrator { get; set; }

    public string CloseButtonBehavior { get; set; } = GestureConfigContract.CloseButtonBehaviors.MinimizeToTray;

    public bool GesturePaused { get; set; }

    public bool DisableGesturesInFullscreen { get; set; }

    public bool DisableEdgeActionsInFullscreen { get; set; }

    public List<ExcludedApplicationConfig> ExcludedApplications { get; set; } = [];
}

public sealed class ExcludedApplicationConfig
{
    public string Name { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Path { get; set; } = "";

    public bool DisableEdgeActions { get; set; }
}

public sealed class WebDavUiSettings
{
    public string Address { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Password { get; set; } = "";

    public string RemotePath { get; set; } = "";
}

public sealed class MouseTrailUiSettings
{
    public bool? Enabled { get; set; } = true;

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
    public bool? Enabled { get; set; } = true;

    public int DisplayDurationMs { get; set; } = 1800;

    public int FadeDurationMs { get; set; } = 240;

    public string FontFamily { get; set; } = "Segoe UI Semibold";

    public float FontSize { get; set; } = 22f;

    public string TextColor { get; set; } = "#FFFFFF";

    public string BackgroundColor { get; set; } = "#12181F";

    public int BackgroundOpacity { get; set; } = 90;

    public int Width { get; set; } = 540;

    public int WidthPercent { get; set; } = 28;

    public bool AutoWidth { get; set; }

    public int Height { get; set; } = 120;

    public int HeightPercent { get; set; } = 11;

    public float CornerRadius { get; set; } = 28f;

    public int BottomOffset { get; set; } = 140;

    public int BottomOffsetPercent { get; set; } = 13;
}

public sealed class LevelOsdUiSettings
{
    public bool? Enabled { get; set; } = true;

    public int DisplayDurationMs { get; set; } = 1800;

    public int FadeDurationMs { get; set; } = 240;

    public string BackgroundColor { get; set; } = "#28282C";

    public int BackgroundOpacity { get; set; } = 88;

    public string TextColor { get; set; } = "#DCDCDC";

    public string TrackColor { get; set; } = "#464646";

    public string VolumeColor { get; set; } = "#64C8FF";

    public string BrightnessColor { get; set; } = "#FFC828";

    public int Width { get; set; } = 210;

    public int Height { get; set; } = 190;

    public int CornerRadius { get; set; } = 22;

    public string Position { get; set; } = GestureConfigContract.LevelOsdPositions.Center;

    public int OffsetX { get; set; }

    public int OffsetY { get; set; }
}

public sealed class GestureSensitivityUiSettings
{
    public int Percent { get; set; } = 110;
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
