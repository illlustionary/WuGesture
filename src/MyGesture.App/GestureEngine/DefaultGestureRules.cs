using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public static class DefaultGestureRules
{
    public static IReadOnlyList<GestureRule> Create()
    {
        return
        [
            new([GestureDirection.Left], GestureConfigContract.Scopes.Global, "返回", new HotkeyAction([Keys.Menu, Keys.Left])),
            new([GestureDirection.Right], GestureConfigContract.Scopes.Global, "前进", new HotkeyAction([Keys.Menu, Keys.Right])),
            new([GestureDirection.Down, GestureDirection.Right], GestureConfigContract.Scopes.Global, "关闭窗口", new WindowControlAction(WindowControlOperation.Close)),
            new([GestureDirection.DownLeft], GestureConfigContract.Scopes.Global, "最小化", new WindowControlAction(WindowControlOperation.Minimize)),
            new([GestureDirection.Down, GestureDirection.Left], GestureConfigContract.Scopes.Global, "Enter", new HotkeyAction([Keys.Enter])),
            new([GestureDirection.UpRight], GestureConfigContract.Scopes.Global, "最大化", new WindowControlAction(WindowControlOperation.ToggleMaximize))
        ];
    }
}
