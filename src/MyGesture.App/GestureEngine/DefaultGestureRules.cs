using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public static class DefaultGestureRules
{
    public static IReadOnlyList<GestureRule> Create()
    {
        return
        [
            new([GestureDirection.Left], "global", "返回", new HotkeyAction([Keys.Menu, Keys.Left])),
            new([GestureDirection.Right], "global", "前进", new HotkeyAction([Keys.Menu, Keys.Right])),
            new([GestureDirection.Down, GestureDirection.Right], "global", "关闭窗口", new WindowControlAction(WindowControlOperation.Close)),
            new([GestureDirection.DownLeft], "global", "最小化", new WindowControlAction(WindowControlOperation.Minimize)),
            new([GestureDirection.Down, GestureDirection.Left], "global", "Enter", new HotkeyAction([Keys.Enter])),
            new([GestureDirection.UpRight], "global", "最大化", new WindowControlAction(WindowControlOperation.ToggleMaximize))
        ];
    }
}
