using System.Text.Json.Serialization;

namespace WuGesture.App.GestureEngine;

public sealed class GestureConfig
{
    public List<GestureRuleConfig> Rules { get; set; } = [];

    public List<GestureApplicationConfig> Applications { get; set; } = [];

    public List<EdgeActionConfig> EdgeActions { get; set; } = [];

    public GestureUiSettings UiSettings { get; set; } = new();
}

public sealed class GestureApplicationConfig
{
    public string Name { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Path { get; set; } = "";

    public List<string> Categories { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }
}

public sealed class GestureRuleConfig
{
    public string Scope { get; set; } = GestureConfigContract.Scopes.Global;

    public string MouseButton { get; set; } = GestureConfigContract.MouseButtons.Right;

    public List<string> Pattern { get; set; } = [];

    public string ActionName { get; set; } = "";

    public GestureActionConfig Action { get; set; } = new();
}

public sealed class GestureActionConfig
{
    public string Type { get; set; } = GestureConfigContract.ActionTypes.Hotkey;

    public List<string> Keys { get; set; } = [];

    public string Operation { get; set; } = "";

    public int Amount { get; set; } = 5;
}

public sealed class EdgeActionConfig
{
    public bool Enabled { get; set; } = true;

    public string TriggerType { get; set; } = GestureConfigContract.EdgeTriggerTypes.Corner;

    public string Location { get; set; } = GestureConfigContract.EdgeLocations.TopLeft;

    public string WheelDirection { get; set; } = "";

    public int FrictionCount { get; set; } = 4;

    public GestureActionConfig Action { get; set; } = new();
}

public sealed record LoadedGestureConfig(
    string FilePath,
    GestureConfig Config,
    IReadOnlyList<GestureRule> Rules);
