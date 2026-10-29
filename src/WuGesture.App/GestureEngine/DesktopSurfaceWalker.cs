namespace WuGesture.App.GestureEngine;

internal static class DesktopSurfaceWalker
{
    internal const int MaxRelatedWindows = 32;

    /// <summary>
    /// 沿父窗口和属主窗口向上找桌面外壳类。Win32 查询通过委托注入，便于单元测试遍历与去重逻辑。
    /// </summary>
    public static bool IsDesktopSurface(
        IntPtr window,
        Func<IntPtr, string> windowClassName,
        Func<IntPtr, IntPtr> parent,
        Func<IntPtr, IntPtr> owner,
        Func<IntPtr, IntPtr> root,
        Func<IntPtr, IntPtr> rootOwner)
    {
        if (window == IntPtr.Zero)
        {
            return false;
        }

        var visited = new HashSet<IntPtr>();
        return IsDesktopSurfaceOrRelated(window, visited, windowClassName, parent, owner) ||
               IsDesktopSurfaceOrRelated(root(window), visited, windowClassName, parent, owner) ||
               IsDesktopSurfaceOrRelated(rootOwner(window), visited, windowClassName, parent, owner);
    }

    public static bool IsDesktopShellClass(string className)
    {
        return className is "Progman" or "WorkerW" or "SHELLDLL_DefView";
    }

    private static bool IsDesktopSurfaceOrRelated(
        IntPtr window,
        ISet<IntPtr> visited,
        Func<IntPtr, string> windowClassName,
        Func<IntPtr, IntPtr> parent,
        Func<IntPtr, IntPtr> owner)
    {
        for (var current = window; current != IntPtr.Zero && visited.Add(current); current = parent(current))
        {
            if (IsDesktopShellClass(windowClassName(current)))
            {
                return true;
            }

            var ownerWindow = owner(current);
            if (ownerWindow != IntPtr.Zero &&
                IsDesktopSurfaceOrRelated(ownerWindow, visited, windowClassName, parent, owner))
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
}
