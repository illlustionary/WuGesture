using System.Drawing;

namespace WuGesture.App.GestureEngine;

/// <summary>
/// 全屏前台判定的纯几何/样式部分，和 Win32 查询分开以便单元测试。
/// </summary>
internal static class FullscreenWindowHeuristics
{
    internal const int EdgeTolerance = 2;
    internal const uint CaptionStyle = 0x00C00000;
    internal const uint ThickFrameStyle = 0x00040000;

    public static bool CoversMonitor(Rectangle window, Rectangle monitor)
    {
        return window.Left <= monitor.Left + EdgeTolerance &&
               window.Top <= monitor.Top + EdgeTolerance &&
               window.Right >= monitor.Right - EdgeTolerance &&
               window.Bottom >= monitor.Bottom - EdgeTolerance;
    }

    public static bool HasCaption(uint style)
    {
        return (style & CaptionStyle) != 0;
    }

    /// <summary>
    /// 最大化但仍然保留可调整边框的窗口是普通桌面窗口；只有无边框最大化或直接铺满才是全屏应用。
    /// </summary>
    public static bool IsFullscreenWithoutMaximizeChrome(uint style, bool zoomed)
    {
        return !zoomed || (style & ThickFrameStyle) == 0;
    }
}
