namespace MyGesture.App.GestureEngine;

public sealed class GestureConfig
{
    public List<GestureRuleConfig> Rules { get; set; } = [];
}

public sealed class GestureRuleConfig
{
    public string Scope { get; set; } = "global";

    public List<string> Pattern { get; set; } = [];

    public string ActionName { get; set; } = "";

    public GestureActionConfig Action { get; set; } = new();
}

public sealed class GestureActionConfig
{
    public string Type { get; set; } = "hotkey";

    public List<string> Keys { get; set; } = [];
}

public sealed record LoadedGestureConfig(
    string FilePath,
    GestureConfig Config,
    IReadOnlyList<GestureRule> Rules);
