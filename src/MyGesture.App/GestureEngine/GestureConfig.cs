namespace MyGesture.App.GestureEngine;

public sealed class GestureConfig
{
    public List<GestureRuleConfig> Rules { get; set; } = [];

    public List<GestureApplicationConfig> Applications { get; set; } = [];
}

public sealed class GestureApplicationConfig
{
    public string Name { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Path { get; set; } = "";

    public string Category { get; set; } = "";
}

public sealed class GestureRuleConfig
{
    public string Scope { get; set; } = "global";

    public string MouseButton { get; set; } = "right";

    public List<string> Pattern { get; set; } = [];

    public string ActionName { get; set; } = "";

    public GestureActionConfig Action { get; set; } = new();
}

public sealed class GestureActionConfig
{
    public string Type { get; set; } = "hotkey";

    public List<string> Keys { get; set; } = [];

    public string Operation { get; set; } = "";
}

public sealed record LoadedGestureConfig(
    string FilePath,
    GestureConfig Config,
    IReadOnlyList<GestureRule> Rules);
