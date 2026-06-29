using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public sealed record GestureRule(
    IReadOnlyList<GestureDirection> Pattern,
    string ActionName,
    HotkeyAction Action);

public sealed record HotkeyAction(IReadOnlyList<Keys> Keys);
