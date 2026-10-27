using System.Drawing;

namespace WuGesture.App;

/// <summary>
/// Window buttons drawn into the custom title bar, in the order they appear from right to left.
/// </summary>
internal enum TitleBarButton
{
    None,
    Minimize,
    Maximize,
    Close
}

/// <summary>
/// Geometry for the custom title bar. Kept free of window handles so the layout and hit testing can be
/// covered by unit tests. All public members take a DPI value so the same code serves every monitor.
/// </summary>
internal static class TitleBarLayout
{
    internal const int BaseDpi = 96;
    internal const int LogicalHeight = 40;
    internal const int LogicalButtonWidth = 46;
    internal const int LogicalIconInset = 12;
    internal const int LogicalIconSize = 16;
    internal const int LogicalTextGap = 8;
    internal const int LogicalResizeEdge = 4;

    internal static int Scale(int logicalPixels, int dpi)
    {
        var safeDpi = dpi <= 0 ? BaseDpi : dpi;
        return (int)Math.Round(logicalPixels * safeDpi / (double)BaseDpi, MidpointRounding.AwayFromZero);
    }

    internal static Rectangle GetButtonBounds(int clientWidth, int height, int dpi, TitleBarButton button)
    {
        var width = Scale(LogicalButtonWidth, dpi);
        var offsetFromRight = button switch
        {
            TitleBarButton.Close => 0,
            TitleBarButton.Maximize => 1,
            TitleBarButton.Minimize => 2,
            _ => -1
        };

        if (offsetFromRight < 0 || width <= 0 || height <= 0)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(clientWidth - (offsetFromRight + 1) * width, 0, width, height);
    }

    internal static TitleBarButton HitTestButton(int clientWidth, int height, int dpi, Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.Y >= height || point.X >= clientWidth)
        {
            return TitleBarButton.None;
        }

        if (GetButtonBounds(clientWidth, height, dpi, TitleBarButton.Close).Contains(point))
        {
            return TitleBarButton.Close;
        }

        if (GetButtonBounds(clientWidth, height, dpi, TitleBarButton.Maximize).Contains(point))
        {
            return TitleBarButton.Maximize;
        }

        return GetButtonBounds(clientWidth, height, dpi, TitleBarButton.Minimize).Contains(point)
            ? TitleBarButton.Minimize
            : TitleBarButton.None;
    }

    /// <summary>
    /// Area between the icon and the window buttons that the title text may occupy.
    /// </summary>
    internal static Rectangle GetTextBounds(int clientWidth, int height, int dpi)
    {
        var left = Scale(LogicalIconInset, dpi) + Scale(LogicalIconSize, dpi) + Scale(LogicalTextGap, dpi);
        var right = clientWidth - Scale(LogicalButtonWidth, dpi) * 3 - Scale(LogicalTextGap, dpi);
        return new Rectangle(left, 0, Math.Max(0, right - left), height);
    }

    internal static Rectangle GetIconBounds(int height, int dpi)
    {
        var size = Scale(LogicalIconSize, dpi);
        return new Rectangle(
            Scale(LogicalIconInset, dpi),
            (height - size) / 2,
            size,
            size);
    }
}
