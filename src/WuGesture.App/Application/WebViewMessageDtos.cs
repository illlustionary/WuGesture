using WuGesture.App.GestureEngine;

namespace WuGesture.App;

internal sealed class RulesWebMessage
{
    public string Type { get; set; } = "";

    public List<GestureRuleConfig> Rules { get; set; } = [];

    public List<GestureApplicationConfig> Applications { get; set; } = [];

    public List<string> Categories { get; set; } = [];

    public List<EdgeActionConfig> EdgeActions { get; set; } = [];

    public GestureUiSettings UiSettings { get; set; } = new();
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
