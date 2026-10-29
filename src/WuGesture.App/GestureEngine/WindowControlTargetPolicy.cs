using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

/// <summary>
/// 决定一个窗口能否接收窗口控制动作（最小化、最大化、置顶、关闭）以及手势动作前的
/// 前台激活。只有真正的应用窗口才允许：
/// <list type="bullet">
/// <item>桌面、任务栏、任务视图、Alt+Tab 切换器、开始菜单、搜索和 Shell 弹层；</item>
/// <item>隐藏窗口、被 DWM 遮蔽的窗口（其他虚拟桌面、挂起的 UWP 应用）；</item>
/// <item>属于 Shell 宿主进程的窗口（explorer 的文件夹窗口除外）。</item>
/// </list>
/// 判定规则在 <see cref="ShellUiWindowHeuristics"/> 中，这里只负责 Win32 句柄和进程查询。
/// </summary>
internal static class WindowControlTargetPolicy
{
    private const uint GetAncestorRoot = 2;
    private const uint GetWindowOwner = 4;
    private const int MaximumRelatedWindows = 32;
    private const int DwmWindowAttributeCloaked = 14;

    public static bool IsWindowControlTarget(IntPtr window)
    {
        if (window == IntPtr.Zero || !IsWindow(window))
        {
            return false;
        }

        // Work with the top-level window: child controls (for example the CoreWindow inside a
        // UWP ApplicationFrameWindow) must not be mistaken for a Shell surface.
        var root = GetAncestor(window, GetAncestorRoot);
        if (root == IntPtr.Zero)
        {
            return false;
        }

        if (!ShellUiWindowHeuristics.IsInteractableWindow(IsWindowVisible(root), IsCloaked(root)))
        {
            return false;
        }

        var visited = new HashSet<IntPtr>();
        for (var current = root;
             current != IntPtr.Zero && visited.Count < MaximumRelatedWindows && visited.Add(current);
             current = GetWindow(current, GetWindowOwner))
        {
            if (DesktopWindowClassifier.IsDesktopSurface(current) ||
                ShellUiWindowHeuristics.IsShellUiWindow(GetWindowClassName(current), GetWindowProcessName(current)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCloaked(IntPtr window)
    {
        return DwmGetWindowAttribute(window, DwmWindowAttributeCloaked, out var cloaked, sizeof(int)) == 0 &&
               cloaked != 0;
    }

    private static string GetWindowProcessName(IntPtr window)
    {
        return WindowProcessCache.TryGet(window, includeExecutablePath: false, out var info)
            ? info.ProcessName
            : string.Empty;
    }

    private static string GetWindowClassName(IntPtr window)
    {
        var buffer = new char[256];
        var length = GetClassName(window, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
}
