using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

internal static class DesktopWindowClassifier
{
    private const uint GetAncestorRoot = 2;
    private const uint GetAncestorRootOwner = 3;
    private const uint GetWindowOwner = 4;

    public static bool IsDesktopSurface(IntPtr window)
    {
        return DesktopSurfaceWalker.IsDesktopSurface(
            window,
            GetWindowClassName,
            GetParent,
            hwnd => GetWindow(hwnd, GetWindowOwner),
            hwnd => GetAncestor(hwnd, GetAncestorRoot),
            hwnd => GetAncestor(hwnd, GetAncestorRootOwner));
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
