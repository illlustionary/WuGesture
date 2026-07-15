using System.Runtime.InteropServices;
using System.Text;

namespace WuGesture.App.GestureEngine;

public static class ForegroundWindowFullscreenDetector
{
    private const int FullscreenEdgeTolerance = 2;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const uint WsCaption = 0x00C00000;
    private const uint WsThickFrame = 0x00040000;

    public static bool IsFullscreenForegroundWindow()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero ||
            !IsWindow(foregroundWindow) ||
            !IsWindowVisible(foregroundWindow) ||
            IsIconic(foregroundWindow) ||
            IsCurrentProcessWindow(foregroundWindow) ||
            IsShellWindow(foregroundWindow))
        {
            return false;
        }

        var windowInfo = new WindowInfo
        {
            CbSize = (uint)Marshal.SizeOf<WindowInfo>()
        };

        if (!GetWindowInfo(foregroundWindow, ref windowInfo))
        {
            return false;
        }

        var monitor = MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MonitorInfo
        {
            CbSize = (uint)Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo) ||
            !CoversMonitor(windowInfo.WindowRect, monitorInfo.MonitorArea))
        {
            return false;
        }

        if ((windowInfo.Style & WsCaption) != 0)
        {
            return false;
        }

        return !IsZoomed(foregroundWindow) || (windowInfo.Style & WsThickFrame) == 0;
    }

    private static bool CoversMonitor(Rect windowBounds, Rect monitorBounds)
    {
        return windowBounds.Left <= monitorBounds.Left + FullscreenEdgeTolerance &&
               windowBounds.Top <= monitorBounds.Top + FullscreenEdgeTolerance &&
               windowBounds.Right >= monitorBounds.Right - FullscreenEdgeTolerance &&
               windowBounds.Bottom >= monitorBounds.Bottom - FullscreenEdgeTolerance;
    }

    private static bool IsCurrentProcessWindow(IntPtr window)
    {
        return GetWindowThreadProcessId(window, out var processId) != 0 &&
               processId == Environment.ProcessId;
    }

    private static bool IsShellWindow(IntPtr window)
    {
        var className = new StringBuilder(256);
        if (GetClassName(window, className, className.Capacity) == 0)
        {
            return false;
        }

        return className.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Progman" or "WorkerW";
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo windowInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowInfo
    {
        public uint CbSize;
        public Rect WindowRect;
        public Rect ClientRect;
        public uint Style;
        public uint ExStyle;
        public uint WindowStatus;
        public uint WindowBorderWidth;
        public uint WindowBorderHeight;
        public ushort WindowType;
        public ushort CreatorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint CbSize;
        public Rect MonitorArea;
        public Rect WorkArea;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
