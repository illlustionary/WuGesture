using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

internal static class DesktopWindowClassifier
{
    private const uint GetAncestorRoot = 2;
    private const uint GetAncestorRootOwner = 3;
    private const uint GetWindowOwner = 4;
    private const int MaxRelatedWindows = 32;

    public static bool IsDesktopSurface(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return false;
        }

        var visited = new HashSet<IntPtr>();
        return IsDesktopSurfaceOrRelated(window, visited) ||
               IsDesktopSurfaceOrRelated(GetAncestor(window, GetAncestorRoot), visited) ||
               IsDesktopSurfaceOrRelated(GetAncestor(window, GetAncestorRootOwner), visited);
    }

    private static bool IsDesktopSurfaceOrRelated(IntPtr window, ISet<IntPtr> visited)
    {
        for (var current = window; current != IntPtr.Zero && visited.Add(current); current = GetParent(current))
        {
            if (IsDesktopShellClass(GetWindowClassName(current)))
            {
                return true;
            }

            var owner = GetWindow(current, GetWindowOwner);
            if (owner != IntPtr.Zero && IsDesktopSurfaceOrRelated(owner, visited))
            {
                return true;
            }

            if (visited.Count >= MaxRelatedWindows)
            {
                break;
            }
        }

        return false;
    }

    private static bool IsDesktopShellClass(string className)
    {
        return className is "Progman" or "WorkerW" or "SHELLDLL_DefView" or "SysListView32";
    }

    private static string GetWindowClassName(IntPtr window)
    {
        var buffer = new char[256];
        var length = GetClassName(window, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);
}
