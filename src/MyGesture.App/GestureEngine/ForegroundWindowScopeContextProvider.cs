using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MyGesture.App.GestureEngine;

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

        return new GestureScopeContext(
            GetProcessName(processId),
            GetWindowClassName(foregroundWindow));
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

    private static string GetWindowClassName(IntPtr windowHandle)
    {
        var builder = new StringBuilder(256);
        if (GetClassName(windowHandle, builder, builder.Capacity) <= 0)
        {
            return "";
        }

        return builder.ToString();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
}
