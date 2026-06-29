using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public static class DefaultGestureRules
{
    public static IReadOnlyList<GestureRule> Create()
    {
        return
        [
            new([GestureDirection.Left], "global", "Back", new HotkeyAction([Keys.Menu, Keys.Left])),
            new([GestureDirection.Right], "global", "Forward", new HotkeyAction([Keys.Menu, Keys.Right])),
            new([GestureDirection.Down, GestureDirection.Right], "global", "Close Tab", new HotkeyAction([Keys.ControlKey, Keys.W]))
        ];
    }
}
