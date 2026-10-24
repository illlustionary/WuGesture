using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

public sealed class ForegroundWindowScopeContextProvider : IGestureScopeContextProvider
{
    public GestureScopeContext GetCurrentContext()
    {
        return GetContextForWindow(GetForegroundWindow());
    }

    public GestureScopeContext GetContextForWindow(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return GestureScopeContext.Empty;
        }

        return WindowProcessCache.TryGet(window, includeExecutablePath: false, out var process)
            ? new GestureScopeContext(process.ProcessName, [])
            : GestureScopeContext.Empty;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
