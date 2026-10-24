using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

/// <summary>
/// Serializer settings shared by every WebView message handler, so the host and the coordinators cannot
/// drift apart on case sensitivity.
/// </summary>
internal static class WebMessageJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

internal sealed class RulesWebMessage
{
    public string Type { get; set; } = "";

    public List<GestureRuleConfig> Rules { get; set; } = [];

    public List<GestureApplicationConfig> Applications { get; set; } = [];

    public List<string> Categories { get; set; } = [];

    public List<EdgeActionConfig> EdgeActions { get; set; } = [];

    /// <summary>
    /// Null when the editor did not send the section, so the coordinator can keep the settings that are
    /// already on disk instead of silently resetting them to defaults.
    /// </summary>
    public GestureUiSettings? UiSettings { get; set; }
}

internal sealed class SelectApplicationWebMessage
{
    public string Type { get; set; } = "";

    public string RequestId { get; set; } = "";

    public string Category { get; set; } = "";
}

internal sealed class OpenApplicationFolderWebMessage
{
    public string Type { get; set; } = "";

    public string Path { get; set; } = "";
}

internal sealed class SetGesturePausedWebMessage
{
    public string Type { get; set; } = "";

    public bool Paused { get; set; }
}

internal sealed class StartHotkeyRecordingWebMessage
{
    public string Type { get; set; } = "";

    public string RequestId { get; set; } = "";
}

internal sealed class StartGestureRecordingWebMessage
{
    public string Type { get; set; } = "";

    public string RequestId { get; set; } = "";
}

internal sealed class PreviewLevelOsdWebMessage
{
    public string Type { get; set; } = "";

    public string Kind { get; set; } = "";
}
