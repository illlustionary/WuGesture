using System.Drawing.Drawing2D;

namespace WuGesture.App;

/// <summary>
/// Colours for the notification-area menu. Both variants mirror the web UI palette so the tray menu matches
/// the configuration window instead of using the stock WinForms gray chrome.
/// </summary>
internal readonly record struct TrayMenuPalette(
    Color Background,
    Color Border,
    Color Text,
    Color MutedText,
    Color Hover,
    Color Pressed,
    Color Accent,
    Color Separator)
{
    public static TrayMenuPalette Light { get; } = new(
        Color.FromArgb(0xFF, 0xFF, 0xFF),
        Color.FromArgb(0xE2, 0xE7, 0xED),
        Color.FromArgb(0x16, 0x20, 0x2B),
        Color.FromArgb(0x8B, 0x95, 0xA3),
        Color.FromArgb(0xF2, 0xF5, 0xF9),
        Color.FromArgb(0xE9, 0xEF, 0xF6),
        Color.FromArgb(0x52, 0x9E, 0xF1),
        Color.FromArgb(0xEC, 0xF0, 0xF4));

    public static TrayMenuPalette Dark { get; } = new(
        Color.FromArgb(0x1A, 0x22, 0x2C),
        Color.FromArgb(0x39, 0x44, 0x51),
        Color.FromArgb(0xED, 0xF2, 0xF7),
        Color.FromArgb(0x7F, 0x8C, 0x9B),
        Color.FromArgb(0x22, 0x2D, 0x38),
        Color.FromArgb(0x2C, 0x38, 0x47),
        Color.FromArgb(0x52, 0x9E, 0xF1),
        Color.FromArgb(0x2A, 0x35, 0x42));
}

/// <summary>
/// Draws the tray context menu with rounded hover rows, flat separators and a matching border. The rounded
/// outer shape comes from <see cref="TrayMenuController"/> clipping the drop-down with a region.
/// </summary>
internal sealed class TrayMenuRenderer : ToolStripRenderer
{
    private const int DotOffset = 10;

    private readonly TrayMenuPalette palette;
    private readonly int leftPadding;
    private readonly int rightPadding;

    public TrayMenuRenderer(TrayMenuPalette palette, int leftPadding, int rightPadding)
    {
        this.palette = palette;
        this.leftPadding = leftPadding;
        this.rightPadding = rightPadding;
    }

    public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 1)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.Clear(palette.Background);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        var bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        using var path = CreateRoundedPath(bounds, Scale(e.ToolStrip, 7));
        using var pen = new Pen(palette.Border);
        var smoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
        e.Graphics.SmoothingMode = smoothing;
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        if (!item.Selected && !item.Pressed)
        {
            return;
        }

        var inset = Scale(e.ToolStrip, 2);
        var width = Math.Min(item.Width, e.ToolStrip?.Width ?? item.Width);
        var bounds = new Rectangle(
            inset,
            inset / 2,
            Math.Max(0, width - inset * 2),
            Math.Max(0, item.Height - inset));
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using var path = CreateRoundedPath(bounds, Scale(e.ToolStrip, 5));
        using var brush = new SolidBrush(item.Pressed ? palette.Pressed : palette.Hover);
        var smoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(brush, path);
        e.Graphics.SmoothingMode = smoothing;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var left = Scale(e.ToolStrip, leftPadding);
        var right = Scale(e.ToolStrip, rightPadding);

        // Check margins are hidden, so the paused toggle marks itself with an accent dot in the reserved
        // space left of the label.
        if (e.Item is ToolStripMenuItem { Checked: true })
        {
            var dotSize = Scale(e.ToolStrip, 6);
            var dot = new Rectangle(
                Math.Max(0, left - Scale(e.ToolStrip, DotOffset)),
                Math.Max(0, (e.Item.Height - dotSize) / 2),
                dotSize,
                dotSize);
            using var brush = new SolidBrush(palette.Accent);
            var smoothing = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(brush, dot);
            e.Graphics.SmoothingMode = smoothing;
        }

        // ToolStrip ignores item padding when it lays out the text, which leaves every label flush against
        // the row edge. Draw the label against the row itself so it starts after the left padding and sits
        // vertically in the middle of the row.
        var textBounds = new Rectangle(
            left,
            0,
            Math.Max(0, e.Item.Width - left - right),
            e.Item.Height);
        TextRenderer.DrawText(
            e.Graphics,
            e.Text,
            e.TextFont,
            textBounds,
            e.Item.Enabled ? palette.Text : palette.MutedText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // The default check glyph belongs to the hidden check margin; the paused state uses the accent dot.
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        // The renderer graphics are translated to the item origin, so draw with item-local coordinates.
        var width = e.Item.Width;
        var height = e.Item.Height;
        var inset = Scale(e.ToolStrip, 10);
        if (width <= inset * 2)
        {
            return;
        }

        using var pen = new Pen(palette.Separator);
        e.Graphics.DrawLine(pen, inset, height / 2, width - inset, height / 2);
    }

    private static int Scale(ToolStrip? toolStrip, int value)
    {
        var dpi = toolStrip?.DeviceDpi ?? 96;
        return (int)Math.Round(value * dpi / 96.0);
    }
}
