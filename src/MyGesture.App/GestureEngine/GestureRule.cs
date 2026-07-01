using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public sealed record GestureRule(
    IReadOnlyList<GestureDirection> Pattern,
    string Scope,
    string ActionName,
    HotkeyAction Action)
{
    public GestureRule(
        IReadOnlyList<GestureDirection> pattern,
        string scope,
        string actionName,
        HotkeyAction action,
        GestureMouseButton mouseButton)
        : this(pattern, scope, actionName, action)
    {
        MouseButton = mouseButton;
    }

    public GestureMouseButton MouseButton { get; init; } = GestureMouseButton.Right;
}

public sealed record HotkeyAction(IReadOnlyList<Keys> Keys);
