using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

public sealed record GestureRule(
    IReadOnlyList<GestureDirection> Pattern,
    string Scope,
    string ActionName,
    GestureAction Action)
{
    public GestureRule(
        IReadOnlyList<GestureDirection> pattern,
        string scope,
        string actionName,
        GestureAction action,
        GestureMouseButton mouseButton)
        : this(pattern, scope, actionName, action)
    {
        MouseButton = mouseButton;
    }

    public GestureMouseButton MouseButton { get; init; } = GestureMouseButton.Right;

    /// <summary>
    /// Wheel modifier that must be applied to the held gesture for this rule to match.
    /// A null value means the rule is triggered by the direction pattern alone.
    /// </summary>
    public GestureWheelDirection? WheelModifier { get; init; }

    /// <summary>
    /// When true, every wheel notch executes the action immediately and keeps executing while
    /// the button is held. When false, the wheel only selects the rule and the action runs once
    /// when the gesture button is released.
    /// </summary>
    public bool ExecuteOnWheel { get; init; }
}

public abstract record GestureAction;

public sealed record HotkeyAction(IReadOnlyList<Keys> Keys) : GestureAction;

public sealed record WindowControlAction(WindowControlOperation Operation) : GestureAction;

public sealed record VolumeControlAction(VolumeControlOperation Operation, int Amount) : GestureAction;

public sealed record BrightnessControlAction(BrightnessControlOperation Operation, int Amount) : GestureAction;

public sealed record ProgramAction(string Path, IReadOnlyList<string> Arguments) : GestureAction;

public enum WindowControlOperation
{
    ToggleTopMost,
    ToggleMaximize,
    Minimize,
    Close
}

public enum VolumeControlOperation
{
    Increase,
    Decrease,
    Mute
}

public enum BrightnessControlOperation
{
    Increase,
    Decrease
}
