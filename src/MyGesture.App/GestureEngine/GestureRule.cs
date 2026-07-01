using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

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
}

public abstract record GestureAction;

public sealed record HotkeyAction(IReadOnlyList<Keys> Keys) : GestureAction;

public sealed record WindowControlAction(WindowControlOperation Operation) : GestureAction;

public enum WindowControlOperation
{
    ToggleTopMost,
    ToggleMaximize,
    Minimize,
    Close
}
