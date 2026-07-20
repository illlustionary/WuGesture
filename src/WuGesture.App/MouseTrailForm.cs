using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

public sealed class MouseTrailForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;
    private const int UlwAlpha = 0x00000002;
    private const int WmNcHitTest = 0x0084;
    private const int HtTransparent = -1;
    private const int FadeFrameIntervalMs = 16;
    private const int HintHorizontalPadding = 28;
    private const int HintMinimumWidth = 240;

    private Pen inactivePen;
    private Pen activePen;
    private Pen dirtyPen;
    private readonly GraphicsPath path = new();
    private readonly GraphicsPath dirtyPath = new();
    private readonly System.Windows.Forms.Timer hintDisplayTimer = new();
    private readonly System.Windows.Forms.Timer hintFadeTimer = new() { Interval = FadeFrameIntervalMs };
    private readonly StringFormat hintTextFormat = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
    };

    private readonly Size bufferSize;
    private readonly IntPtr screenDc;
    private readonly IntPtr memDc;
    private readonly IntPtr dibSection;
    private readonly IntPtr oldBitmap;
    private readonly Graphics graphics;

    private Rectangle screenBounds;
    private PointF lastPoint;
    private bool hasLastPoint;
    private bool isHighlighted;
    private MouseTrailUiSettings uiSettings = new();
    private GestureHintUiSettings hintUiSettings = new();
    private Font? hintFont;
    private Brush? hintTextBrush;
    private Brush? hintBackgroundBrush;
    private Pen? hintBorderPen;
    private string hintTitle = "";
    private Point hintAnchor;
    private bool hasHint;
    private long hintFadeStartedAt;
    private byte windowOpacity = 255;

    public MouseTrailForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.Black;
        Location = SystemInformation.VirtualScreen.Location;
        Bounds = SystemInformation.VirtualScreen;
        screenBounds = SystemInformation.VirtualScreen;
        bufferSize = screenBounds.Size;

        var dpiFactor = Math.Max(1f, DeviceDpi / 96f);
        var pathWidth = 3f * dpiFactor;

        inactivePen = CreatePen(Color.FromArgb(255, 170, 170, 170), pathWidth);
        activePen = CreatePen(Color.SkyBlue, pathWidth);
        dirtyPen = CreatePen(Color.White, pathWidth * 3.5f);

        screenDc = GetDC(IntPtr.Zero);
        memDc = CreateCompatibleDC(screenDc);
        dibSection = CreateDibSection(memDc, bufferSize);
        oldBitmap = SelectObject(memDc, dibSection);
        graphics = Graphics.FromHdc(memDc);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        ApplySettings(uiSettings);
        ApplyHintSettings(hintUiSettings);
        hintDisplayTimer.Tick += OnHintDisplayTimerTick;
        hintFadeTimer.Tick += OnHintFadeTimerTick;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExNoActivate | WsExToolWindow | WsExTransparent | WsExLayered;
            return createParams;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmNcHitTest)
        {
            m.Result = new IntPtr(HtTransparent);
            return;
        }

        base.WndProc(ref m);
    }

    public void Preload()
    {
        if (IsDisposed)
        {
            return;
        }

        _ = Handle;
        graphics.Clear(Color.Transparent);
        Present(new Rectangle(Point.Empty, bufferSize), fullWindow: true);
    }

    public void ApplySettings(MouseTrailUiSettings? settings)
    {
        uiSettings = settings ?? new MouseTrailUiSettings();
        windowOpacity = 255;

        var dpiFactor = Math.Max(1f, DeviceDpi / 96f);
        var inactivePathWidth = Math.Max(1f, uiSettings.InactiveThickness) * dpiFactor;
        var activePathWidth = Math.Max(1f, uiSettings.ActiveThickness) * dpiFactor;
        var inactiveOpacity = ClampOpacity(uiSettings.InactiveOpacity);
        var activeOpacity = ClampOpacity(uiSettings.ActiveOpacity);
        var inactiveColor = ApplyOpacity(GestureColorParser.Parse(uiSettings.InactiveColor, Color.FromArgb(255, 170, 170, 170)), inactiveOpacity);
        var activeColor = ApplyOpacity(GestureColorParser.Parse(uiSettings.ActiveColor, Color.SkyBlue), activeOpacity);

        inactivePen?.Dispose();
        activePen?.Dispose();
        dirtyPen?.Dispose();

        inactivePen = CreatePen(inactiveColor, inactivePathWidth);
        activePen = CreatePen(activeColor, activePathWidth);
        dirtyPen = CreatePen(Color.White, Math.Max(inactivePathWidth, activePathWidth) * 3.5f);

        if (Visible)
        {
            RedrawOverlay();
        }
    }

    public void ApplyHintSettings(GestureHintUiSettings? settings)
    {
        hintUiSettings = settings ?? new GestureHintUiSettings();

        hintFont?.Dispose();
        hintTextBrush?.Dispose();
        hintBackgroundBrush?.Dispose();
        hintBorderPen?.Dispose();

        var baseBackgroundColor = GestureColorParser.Parse(hintUiSettings.BackgroundColor, Color.FromArgb(18, 24, 31));
        var backgroundOpacity = ClampOpacity(hintUiSettings.BackgroundOpacity);
        hintFont = CreateHintFont(hintUiSettings.FontFamily, hintUiSettings.FontSize);
        hintTextBrush = new SolidBrush(GestureColorParser.Parse(hintUiSettings.TextColor, Color.White));
        hintBackgroundBrush = new SolidBrush(ApplyOpacity(baseBackgroundColor, backgroundOpacity));
        hintBorderPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1.1f);

        if (Visible)
        {
            RedrawOverlay();
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        RedrawOverlay();
    }

    public void ShowGestureHint(string ruleName, Point anchor, bool autoHide)
    {
        if (IsDisposed)
        {
            return;
        }

        StopHintTimers();
        windowOpacity = 255;
        hintTitle = string.IsNullOrWhiteSpace(ruleName) ? "已触发" : ruleName;
        hintAnchor = anchor;
        hasHint = true;

        if (!Visible)
        {
            Show();
        }

        RedrawOverlay();
        if (autoHide)
        {
            hintDisplayTimer.Interval = hintUiSettings.DisplayDurationMs;
            hintDisplayTimer.Start();
        }
    }

    public void ClearGestureHint()
    {
        if (IsDisposed || !hasHint)
        {
            return;
        }

        StopHintTimers();
        hasHint = false;
        hintTitle = "";
        RedrawOverlay();

        if (path.PointCount == 0)
        {
            HideTrail();
        }
    }

    public void EndPath()
    {
        if (IsDisposed)
        {
            return;
        }

        hasLastPoint = false;
        isHighlighted = false;
        path.Reset();

        if (!hasHint)
        {
            HideTrail();
            return;
        }

        RedrawOverlay();
    }

    public void ShowPath(IReadOnlyList<Point> points, GestureMouseButton button)
    {
        if (IsDisposed || points.Count < 2)
        {
            HideTrail();
            return;
        }

        if (hintDisplayTimer.Enabled || hintFadeTimer.Enabled)
        {
            StopHintTimers();
            windowOpacity = 255;
            hasHint = false;
            hintTitle = "";
            RedrawOverlay();
        }

        var current = ToLocalPoint(points[^1]);

        if (!Visible)
        {
            Show();
            graphics.Clear(Color.Transparent);
            path.Reset();
            hasLastPoint = false;
        }

        if (!hasLastPoint)
        {
            hasLastPoint = true;
            lastPoint = current;
            Present(new Rectangle((int)current.X, (int)current.Y, 1, 1));
            return;
        }

        if (DistanceSquared(lastPoint, current) < 0.25f)
        {
            return;
        }

        var dirtyRect = DrawSegment(lastPoint, current);
        lastPoint = current;
        Present(dirtyRect);
    }

    public void HideTrail()
    {
        if (IsDisposed)
        {
            return;
        }

        hasLastPoint = false;
        isHighlighted = false;
        hasHint = false;
        hintTitle = "";
        StopHintTimers();
        windowOpacity = 255;
        path.Reset();
        graphics.Clear(Color.Transparent);

        if (Visible)
        {
            Present(new Rectangle(Point.Empty, bufferSize), fullWindow: true);
            Hide();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            graphics.Dispose();
            SelectObject(memDc, oldBitmap);
            DeleteObject(dibSection);
            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
            path.Dispose();
            dirtyPath.Dispose();
            StopHintTimers();
            hintDisplayTimer.Tick -= OnHintDisplayTimerTick;
            hintDisplayTimer.Dispose();
            hintFadeTimer.Tick -= OnHintFadeTimerTick;
            hintFadeTimer.Dispose();
            hintTextFormat.Dispose();
            inactivePen.Dispose();
            activePen.Dispose();
            dirtyPen.Dispose();
            hintFont?.Dispose();
            hintTextBrush?.Dispose();
            hintBackgroundBrush?.Dispose();
            hintBorderPen?.Dispose();
        }

        base.Dispose(disposing);
    }

    private Rectangle DrawSegment(PointF from, PointF to)
    {
        var pen = isHighlighted ? activePen : inactivePen;
        path.AddLine(from, to);
        graphics.DrawLine(pen, from, to);

        dirtyPath.Reset();
        dirtyPath.AddLine(from, to);
        dirtyPath.Widen(dirtyPen);

        var dirtyRect = Rectangle.Ceiling(dirtyPath.GetBounds());
        dirtyRect.Intersect(new Rectangle(Point.Empty, bufferSize));
        return dirtyRect;
    }

    private void RedrawOverlay()
    {
        if (IsDisposed)
        {
            return;
        }

        graphics.Clear(Color.Transparent);
        if (path.PointCount > 0)
        {
            graphics.DrawPath(isHighlighted ? activePen : inactivePen, path);
        }

        if (hasHint)
        {
            DrawHint();
        }

        if (Visible)
        {
            Present(new Rectangle(Point.Empty, bufferSize), fullWindow: true);
        }
    }

    private void DrawHint()
    {
        if (hintFont is null || hintTextBrush is null || hintBackgroundBrush is null || hintBorderPen is null)
        {
            return;
        }

        var area = Screen.FromPoint(hintAnchor).WorkingArea;
        var maxWidth = Math.Max(HintMinimumWidth, area.Width - 24);
        var width = hintUiSettings.AutoWidth
            ? Math.Min(maxWidth, Math.Max(HintMinimumWidth, MeasureHintWidth(hintTitle) + HintHorizontalPadding * 2))
            : Math.Max(HintMinimumWidth, ResolvePercent(area.Width, hintUiSettings.WidthPercent, HintMinimumWidth, area.Width));
        var height = Math.Max(72, ResolvePercent(area.Height, hintUiSettings.HeightPercent, 72, area.Height));
        var bottomOffset = ResolvePercent(area.Height, hintUiSettings.BottomOffsetPercent, 0, area.Height);
        var bounds = new Rectangle(
            area.Left - screenBounds.Left + (area.Width - width) / 2,
            area.Bottom - screenBounds.Top - height - bottomOffset,
            width,
            height);

        using var bubblePath = RoundedRect(bounds, Math.Min(Math.Min(width, height) / 2f, Math.Max(0f, hintUiSettings.CornerRadius)));
        graphics.FillPath(hintBackgroundBrush, bubblePath);
        graphics.DrawPath(hintBorderPen, bubblePath);

        var titleBounds = new RectangleF(
            bounds.Left + HintHorizontalPadding,
            bounds.Top,
            bounds.Width - HintHorizontalPadding * 2,
            bounds.Height);
        hintTextFormat.Trimming = hintUiSettings.AutoWidth ? StringTrimming.None : StringTrimming.EllipsisCharacter;
        graphics.DrawString(hintTitle, hintFont, hintTextBrush, titleBounds, hintTextFormat);
    }

    private int MeasureHintWidth(string text)
    {
        var measuredSize = graphics.MeasureString(string.IsNullOrWhiteSpace(text) ? "已触发" : text, hintFont!);
        return (int)Math.Ceiling(measuredSize.Width) + 8;
    }

    private void OnHintDisplayTimerTick(object? sender, EventArgs e)
    {
        hintDisplayTimer.Stop();
        if (!hasHint || hintUiSettings.FadeDurationMs <= 0)
        {
            HideTrail();
            return;
        }

        hintFadeStartedAt = Environment.TickCount64;
        hintFadeTimer.Start();
    }

    private void OnHintFadeTimerTick(object? sender, EventArgs e)
    {
        if (!hasHint)
        {
            HideTrail();
            return;
        }

        var elapsed = Environment.TickCount64 - hintFadeStartedAt;
        var progress = Math.Min(1d, elapsed / (double)hintUiSettings.FadeDurationMs);
        windowOpacity = (byte)Math.Round(255d * (1d - progress));
        Present(new Rectangle(Point.Empty, bufferSize), fullWindow: true);

        if (progress >= 1d)
        {
            HideTrail();
        }
    }

    private void StopHintTimers()
    {
        hintDisplayTimer.Stop();
        hintFadeTimer.Stop();
    }

    private void Present(Rectangle dirtyRect, bool fullWindow = false)
    {
        if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
        {
            dirtyRect = new Rectangle(0, 0, bufferSize.Width, bufferSize.Height);
        }

        var dstPoint = new Point(screenBounds.Left, screenBounds.Top);
        var srcPoint = new Point(0, 0);
        var size = new Size(bufferSize.Width, bufferSize.Height);
        var blend = new BlendFunction
        {
            BlendOp = 0,
            BlendFlags = 0,
            SourceConstantAlpha = windowOpacity,
            AlphaFormat = 1
        };

        if (fullWindow)
        {
            UpdateLayeredWindow(
                Handle,
                screenDc,
                ref dstPoint,
                ref size,
                memDc,
                ref srcPoint,
                0,
                ref blend,
                UlwAlpha);
            return;
        }

        PresentDirty(dirtyRect, dstPoint, size, srcPoint, blend);
    }

    private unsafe void PresentDirty(
        Rectangle dirtyRect,
        Point destination,
        Size size,
        Point source,
        BlendFunction blend)
    {
        var dirty = new NativeRect(dirtyRect.Left, dirtyRect.Top, dirtyRect.Right, dirtyRect.Bottom);
        var updateInfo = new UpdateLayeredWindowInfo
        {
            Size = (uint)Marshal.SizeOf<UpdateLayeredWindowInfo>(),
            DestinationDc = screenDc,
            SourceDc = memDc,
            ColorKey = 0,
            Flags = UlwAlpha,
            Dirty = &dirty,
            DestinationPoint = &destination,
            WindowSize = &size,
            SourcePoint = &source,
            Blend = &blend
        };

        UpdateLayeredWindowIndirect(Handle, ref updateInfo);
    }

    private static Color ApplyOpacity(Color color, byte opacity)
    {
        return Color.FromArgb(opacity, color.R, color.G, color.B);
    }

    private static byte ClampOpacity(int value)
    {
        var scaled = (int)Math.Round(value * 255d / 100d);
        return (byte)Math.Max(0, Math.Min(255, scaled));
    }

    private static int ResolvePercent(int size, int percent, int min, int max)
    {
        var value = (int)Math.Round(size * Math.Max(0, percent) / 100d);
        return Math.Max(min, Math.Min(max, value));
    }

    private static GraphicsPath RoundedRect(Rectangle rect, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var diameter = radius * 2;
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Font CreateHintFont(string? familyName, float size)
    {
        var resolvedFamily = string.IsNullOrWhiteSpace(familyName) ? "Segoe UI Semibold" : familyName.Trim();
        var resolvedSize = Math.Max(8f, size);

        try
        {
            return new Font(resolvedFamily, resolvedSize, FontStyle.Bold);
        }
        catch
        {
            return new Font("Segoe UI Semibold", resolvedSize, FontStyle.Bold);
        }
    }

    private PointF ToLocalPoint(Point point)
    {
        return new PointF(point.X - screenBounds.Left, point.Y - screenBounds.Top);
    }

    private static float DistanceSquared(PointF a, PointF b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static Pen CreatePen(Color color, float width)
    {
        return new Pen(color, width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
    }

    private static IntPtr CreateDibSection(IntPtr hdc, Size size)
    {
        var bmi = new BitmapInfo();
        bmi.Header.Size = (uint)Marshal.SizeOf<BitmapInfoHeader>();
        bmi.Header.Width = size.Width;
        bmi.Header.Height = -size.Height;
        bmi.Header.Planes = 1;
        bmi.Header.BitCount = 32;
        bmi.Header.Compression = 0;

        var bits = IntPtr.Zero;
        var handle = CreateDIBSection(hdc, ref bmi, 0, out bits, IntPtr.Zero, 0);
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create DIB section: {Marshal.GetLastWin32Error()}");
        }

        return handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect(int left, int top, int right, int bottom)
    {
        public readonly int Left = left;
        public readonly int Top = top;
        public readonly int Right = right;
        public readonly int Bottom = bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct UpdateLayeredWindowInfo
    {
        public uint Size;
        public IntPtr DestinationDc;
        public Point* DestinationPoint;
        public Size* WindowSize;
        public IntPtr SourceDc;
        public Point* SourcePoint;
        public int ColorKey;
        public BlendFunction* Blend;
        public int Flags;
        public NativeRect* Dirty;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateDIBSection(
        IntPtr hdc,
        ref BitmapInfo pbmi,
        uint usage,
        out IntPtr ppvBits,
        IntPtr hSection,
        uint offset);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(
        IntPtr hwnd,
        IntPtr hdcDst,
        ref Point pptDst,
        ref Size psize,
        IntPtr hdcSrc,
        ref Point pptSrc,
        int crKey,
        ref BlendFunction pblend,
        int dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindowIndirect(
        IntPtr hwnd,
        ref UpdateLayeredWindowInfo updateInfo);
}
