using System.Diagnostics;
using System.Runtime.InteropServices;
namespace WuGesture.App.GestureEngine;

public sealed class ForegroundWindowScopeContextProvider : IGestureScopeContextProvider
{
    public GestureScopeContext GetCurrentContext()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return GestureScopeContext.Empty;
        }

        GetWindowThreadProcessId(foregroundWindow, out var processId);

        return new GestureScopeContext(GetProcessName(processId), []);
    }

    private static string GetProcessName(int processId)
    {
        if (processId <= 0)
        {
            return "";
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return "";
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);
}
