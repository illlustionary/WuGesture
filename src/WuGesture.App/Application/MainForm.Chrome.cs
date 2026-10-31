using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WuGesture.App;

/// <summary>
/// The configuration window draws its own title bar so the caption colors from
/// <c>uiSettings.appearance</c> look the same on Windows 10 and Windows 11. Windows only exposes
/// <c>DWMWA_CAPTION_COLOR</c> from build 22000 on, so the previous approach silently fell back to the
/// system caption on Windows 10.
///
/// The window keeps its native resize frame: <c>WM_NCCALCSIZE</c> only extends the client area up over
/// the caption, leaving the left/right/bottom frame (and their resize grips, shadow and snap animation)
/// intact. The title bar itself is painted into the client area rather than a child control, so the form
/// still owns the hit testing and Windows can run its native caption behaviour (drag, Aero Snap,
/// double-click maximize, system menu and the Windows 11 snap-layout flyout over the maximize button).
/// </summary>
public sealed partial class MainForm
{
    private const int WmNcCalcSize = 0x0083;
    private const int WmNcHitTest = 0x0084;
    private const int WmNcActivate = 0x0086;
    private const int WmNcMouseMove = 0x00A0;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonUp = 0x00A2;
    private const int WmNcLButtonDblClk = 0x00A3;
    private const int WmNcMouseLeave = 0x02A2;
    private const int WmLButtonUp = 0x0202;
    private const int WmCaptureChanged = 0x0215;
    private const int HtCaption = 2;
    private const int HtMinButton = 8;
    private const int HtMaxButton = 9;
    private const int HtTop = 12;
    private const int HtClose = 20;
    private const int SystemMetricCyCaption = 4;
    private const int SystemMetricCySizeFrame = 33;
    private const int SystemMetricCxPaddedBorder = 92;
    private const uint TrackMouseEventLeaf = 0x00000002;
    private const uint TrackMouseEventNonClient = 0x00000010;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const string GlyphFontFamily = "Segoe MDL2 Assets";
    private const string MinimizeGlyph = "\uE921";
    private const string MaximizeGlyph = "\uE922";
    private const string RestoreGlyph = "\uE923";
    private const string CloseGlyph = "\uE8BB";

    private static readonly Color CloseButtonHoverColor = Color.FromArgb(196, 43, 28);
    private static readonly Color FallbackTitleBarColor = Color.FromArgb(27, 34, 43);
    private static readonly Color FallbackTitleBarTextColor = Color.FromArgb(237, 242, 247);

    private Panel? contentHost;
    private Color titleBarColor = FallbackTitleBarColor;
    private Color titleBarTextColor = FallbackTitleBarTextColor;
    private bool useDarkTitleBar = true;
    private bool isTitleBarActive = true;
    private TitleBarButton hoveredTitleBarButton = TitleBarButton.None;
    private TitleBarButton pressedTitleBarButton = TitleBarButton.None;
    private Font? titleBarFont;
    private Font? titleBarGlyphFont;

    private int TitleBarHeight => TitleBarLayout.Scale(TitleBarLayout.LogicalHeight, DeviceDpi);

    /// <summary>
    /// Paints the whole title bar into an offscreen buffer and skips the separate background erase.
    /// Without it, every hover change repaints the strip in place and Windows can compose the frame between
    /// the background fill and the icon/text/button drawing, which makes the whole caption blink.
    /// </summary>
    private void ConfigureTitleBarPainting()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        UpdateStyles();
    }

    /// <summary>
    /// Hosts the WebView so the painted title bar keeps its own strip above the web content.
    /// </summary>
    private Panel CreateContentHost()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = titleBarColor
        };
        Controls.Add(host);
        contentHost = host;
        return host;
    }

    private void ApplyTitleBarTheme(Color captionColor, Color textColor, bool useDarkPalette)
    {
        titleBarColor = captionColor;
        titleBarTextColor = textColor;
        useDarkTitleBar = useDarkPalette;
        if (contentHost is not null)
        {
            contentHost.BackColor = captionColor;
        }

        ApplyDwmBorderColor(captionColor, textColor);
        InvalidateTitleBar();
    }

    /// <summary>
    /// Windows 11 paints a one pixel window border; matching it to the caption keeps the custom title bar
    /// from looking like it floats over the desktop. Windows 10 ignores the attribute.
    /// </summary>
    private void ApplyDwmBorderColor(Color captionColor, Color textColor)
    {
        if (!IsHandleCreated || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var borderColor = Blend(captionColor, textColor, 0.22f);
        var nativeBorderColor = ColorTranslator.ToWin32(borderColor);
        var nativeCaptionColor = ColorTranslator.ToWin32(captionColor);
        var nativeTextColor = ColorTranslator.ToWin32(textColor);
        _ = DwmSetWindowAttribute(Handle, DwmwaBorderColor, ref nativeBorderColor, sizeof(int));
        _ = DwmSetWindowAttribute(Handle, DwmwaCaptionColor, ref nativeCaptionColor, sizeof(int));
        _ = DwmSetWindowAttribute(Handle, DwmwaTextColor, ref nativeTextColor, sizeof(int));
    }

    private void ApplyTitleBarMetrics()
    {
        var height = TitleBarHeight;
        if (Padding.Top != height)
        {
            Padding = new Padding(Padding.Left, height, Padding.Right, Padding.Bottom);
        }

        var fontFamily = Font?.FontFamily ?? SystemFonts.MessageBoxFont?.FontFamily;
        titleBarFont?.Dispose();
        titleBarFont = fontFamily is null ? null : new Font(fontFamily, 10f, FontStyle.Regular, GraphicsUnit.Point);
        titleBarGlyphFont?.Dispose();
        titleBarGlyphFont = CreateGlyphFont();
        InvalidateTitleBar();
    }

    private static Font? CreateGlyphFont()
    {
        try
        {
            return new Font(GlyphFontFamily, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private void InvalidateTitleBar()
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        Invalidate(new Rectangle(0, 0, ClientSize.Width, TitleBarHeight));
    }

    private void DisposeTitleBarFonts()
    {
        titleBarFont?.Dispose();
        titleBarFont = null;
        titleBarGlyphFont?.Dispose();
        titleBarGlyphFont = null;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintTitleBar(e.Graphics);
    }

    private void PaintTitleBar(Graphics graphics)
    {
        var width = ClientSize.Width;
        var height = TitleBarHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var textColor = EffectiveTitleBarTextColor;
        using (var background = new SolidBrush(titleBarColor))
        {
            graphics.FillRectangle(background, 0, 0, width, height);
        }

        // A hairline keeps the strip readable when the caption color matches the page background.
        using (var separator = new Pen(Blend(titleBarColor, textColor, 0.16f)))
        {
            graphics.DrawLine(separator, 0, height - 1, width, height - 1);
        }

        var dpi = DeviceDpi;
        var iconBounds = TitleBarLayout.GetIconBounds(height, dpi);
        if (Icon is not null && iconBounds.Width > 0)
        {
            var previousMode = graphics.InterpolationMode;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawIcon(Icon, iconBounds);
            graphics.InterpolationMode = previousMode;
        }

        if (titleBarFont is not null)
        {
            TextRenderer.DrawText(
                graphics,
                Text,
                titleBarFont,
                TitleBarLayout.GetTextBounds(width, height, dpi),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }

        PaintTitleBarButton(graphics, TitleBarButton.Minimize, MinimizeGlyph);
        PaintTitleBarButton(graphics, TitleBarButton.Maximize, IsZoomed(Handle) ? RestoreGlyph : MaximizeGlyph);
        PaintTitleBarButton(graphics, TitleBarButton.Close, CloseGlyph);
    }

    private void PaintTitleBarButton(Graphics graphics, TitleBarButton button, string glyph)
    {
        var bounds = TitleBarLayout.GetButtonBounds(ClientSize.Width, TitleBarHeight, DeviceDpi, button);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var isHovered = isTitleBarActive && hoveredTitleBarButton == button;
        if (isHovered)
        {
            using var hover = new SolidBrush(
                button == TitleBarButton.Close
                    ? CloseButtonHoverColor
                    : Blend(titleBarColor, titleBarTextColor, useDarkTitleBar ? 0.16f : 0.1f));
            graphics.FillRectangle(hover, bounds);
        }

        if (titleBarGlyphFont is null)
        {
            return;
        }

        var glyphColor = isHovered && button == TitleBarButton.Close ? Color.White : EffectiveTitleBarTextColor;
        TextRenderer.DrawText(
            graphics,
            glyph,
            titleBarGlyphFont,
            bounds,
            glyphColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPadding);
    }

    private Color EffectiveTitleBarTextColor =>
        isTitleBarActive ? titleBarTextColor : Blend(titleBarColor, titleBarTextColor, 0.55f);

    private static Color Blend(Color background, Color foreground, float amount)
    {
        var ratio = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            background.A,
            (int)Math.Round(background.R + (foreground.R - background.R) * ratio),
            (int)Math.Round(background.G + (foreground.G - background.G) * ratio),
            (int)Math.Round(background.B + (foreground.B - background.B) * ratio));
    }

    private void UpdateTitleBarHover(Point clientPoint)
    {
        SetTitleBarHover(TitleBarLayout.HitTestButton(ClientSize.Width, TitleBarHeight, DeviceDpi, clientPoint));
    }

    private void SetTitleBarHover(TitleBarButton button)
    {
        if (hoveredTitleBarButton == button)
        {
            return;
        }

        var previous = hoveredTitleBarButton;
        hoveredTitleBarButton = button;

        // Only the two buttons whose highlight changed need a repaint. Invalidating the whole strip on every
        // crossing made the caption redraw while the pointer moved across the window buttons.
        InvalidateTitleBarButtons(previous, button);
    }

    private void InvalidateTitleBarButtons(TitleBarButton first, TitleBarButton second)
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        var region = GetTitleBarButtonBounds(first);
        var secondBounds = GetTitleBarButtonBounds(second);
        if (!secondBounds.IsEmpty)
        {
            region = region.IsEmpty ? secondBounds : Rectangle.Union(region, secondBounds);
        }

        if (region.IsEmpty)
        {
            return;
        }

        Invalidate(Rectangle.Inflate(region, 1, 1));
    }

    private Rectangle GetTitleBarButtonBounds(TitleBarButton button)
    {
        if (button == TitleBarButton.None)
        {
            return Rectangle.Empty;
        }

        var bounds = TitleBarLayout.GetButtonBounds(ClientSize.Width, TitleBarHeight, DeviceDpi, button);
        return bounds.Width <= 0 || bounds.Height <= 0 ? Rectangle.Empty : bounds;
    }

    /// <summary>
    /// <c>WM_NCMOUSELEAVE</c> is only posted after an explicit <c>TrackMouseEvent</c> request, so ask for it on
    /// every non-client move; otherwise a hovered button stays highlighted after the pointer leaves the window.
    /// </summary>
    private void TrackNonClientMouseLeave()
    {
        var track = new TrackMouseEventData
        {
            Size = Marshal.SizeOf<TrackMouseEventData>(),
            Flags = TrackMouseEventLeaf | TrackMouseEventNonClient,
            Window = Handle
        };
        _ = TrackMouseEvent(ref track);
    }

    /// <summary>
    /// Extends the client area over the caption so nothing is drawn by the system, while the standard
    /// frame stays in place.
    /// </summary>
    private void AdjustClientAreaForTitleBar(ref Message message)
    {
        // Let Windows compute the standard non-client layout first.
        base.WndProc(ref message);

        var parameters = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(message.LParam);
        var dpi = GetDpiForWindow(Handle);
        var captionHeight = GetSystemMetricsForDpi(SystemMetricCyCaption, dpi);
        var topFrame = GetSystemMetricsForDpi(SystemMetricCySizeFrame, dpi) +
            GetSystemMetricsForDpi(SystemMetricCxPaddedBorder, dpi);

        // A maximized window is inflated past the monitor so its resize frame is hidden; only the caption
        // has to be removed then. A normal window additionally has to swallow the top frame.
        parameters.rgrc0.Top -= IsZoomed(Handle) ? captionHeight : captionHeight + topFrame;
        Marshal.StructureToPtr(parameters, message.LParam, false);
        message.Result = IntPtr.Zero;
    }

    private int HitTestTitleBar(ref Message message)
    {
        var clientPoint = PointToClient(GetScreenPoint(message.LParam));
        var height = TitleBarHeight;
        if (clientPoint.X < 0 || clientPoint.Y < 0 ||
            clientPoint.X >= ClientSize.Width || clientPoint.Y >= height)
        {
            return 0;
        }

        var button = TitleBarLayout.HitTestButton(ClientSize.Width, height, DeviceDpi, clientPoint);
        if (button != TitleBarButton.None)
        {
            return button switch
            {
                TitleBarButton.Minimize => HtMinButton,
                TitleBarButton.Maximize => HtMaxButton,
                _ => HtClose
            };
        }

        // The native top frame is gone after WM_NCCALCSIZE, so the top edge keeps its own resize strip.
        if (!IsZoomed(Handle) && clientPoint.Y < TitleBarLayout.Scale(TitleBarLayout.LogicalResizeEdge, DeviceDpi))
        {
            return HtTop;
        }

        return HtCaption;
    }

    /// <summary>
    /// Remembers which title-bar button the pointer went down on. Nothing happens on press: the caption
    /// buttons only fire when the pointer is released while still over the same button, like native ones.
    /// </summary>
    private bool BeginTitleBarButtonPress(Point clientPoint)
    {
        var button = HitTestTitleBarButton(clientPoint);
        if (button == TitleBarButton.None)
        {
            return false;
        }

        pressedTitleBarButton = button;
        _ = SetCapture(Handle);
        return true;
    }

    private bool CompleteTitleBarButtonPress(Point clientPoint)
    {
        var pressed = pressedTitleBarButton;
        if (pressed == TitleBarButton.None)
        {
            return false;
        }

        pressedTitleBarButton = TitleBarButton.None;
        _ = ReleaseCapture();
        if (HitTestTitleBarButton(clientPoint) != pressed)
        {
            return true;
        }

        SetTitleBarHover(TitleBarButton.None);
        switch (pressed)
        {
            case TitleBarButton.Minimize:
                MinimizeWindow();
                break;
            case TitleBarButton.Maximize:
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
                break;
            case TitleBarButton.Close:
                Close();
                break;
        }

        return true;
    }

    private TitleBarButton HitTestTitleBarButton(Point clientPoint)
    {
        return TitleBarLayout.HitTestButton(ClientSize.Width, TitleBarHeight, DeviceDpi, clientPoint);
    }

    private static Point GetScreenPoint(IntPtr lParam)
    {
        var value = lParam.ToInt64();
        return new Point(
            unchecked((short)(value & 0xFFFF)),
            unchecked((short)((value >> 16) & 0xFFFF)));
    }

    protected override void WndProc(ref Message message)
    {
        switch (message.Msg)
        {
            case WmNcCalcSize when message.WParam != IntPtr.Zero:
                AdjustClientAreaForTitleBar(ref message);
                return;
            case WmNcHitTest:
                var hitCode = HitTestTitleBar(ref message);
                if (hitCode != 0)
                {
                    message.Result = hitCode;
                    return;
                }

                break;
            case WmNcMouseMove:
                TrackNonClientMouseLeave();
                UpdateTitleBarHover(PointToClient(GetScreenPoint(message.LParam)));
                break;
            case WmNcMouseLeave:
                SetTitleBarHover(TitleBarButton.None);
                break;
            case WmNcLButtonDown:
            case WmNcLButtonDblClk:
                // Pressing a caption button only arms it; the action runs on the matching button release.
                if (BeginTitleBarButtonPress(PointToClient(GetScreenPoint(message.LParam))))
                {
                    return;
                }

                break;
            case WmNcLButtonUp:
                if (CompleteTitleBarButtonPress(PointToClient(GetScreenPoint(message.LParam))))
                {
                    return;
                }

                break;
            case WmLButtonUp:
                // With the mouse captured, a release that drifted off the title bar arrives as a client message.
                // Its coordinates are client based, so they need no conversion.
                if (CompleteTitleBarButtonPress(GetScreenPoint(message.LParam)))
                {
                    return;
                }

                break;
            case WmCaptureChanged:
                pressedTitleBarButton = TitleBarButton.None;
                break;
            case WmNcActivate:
                var isActive = message.WParam != IntPtr.Zero;
                if (isTitleBarActive != isActive)
                {
                    isTitleBarActive = isActive;
                    InvalidateTitleBar();
                }

                break;
        }

        base.WndProc(ref message);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        public NativeRectangle rgrc0;
        public NativeRectangle rgrc1;
        public NativeRectangle rgrc2;
        public IntPtr lppos;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TrackMouseEventData
    {
        public int Size;
        public uint Flags;
        public IntPtr Window;
        public uint HoverTime;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool TrackMouseEvent(ref TrackMouseEventData track);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCapture(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
}
