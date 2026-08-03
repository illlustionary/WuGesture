using System.Drawing.Drawing2D;
using System.Diagnostics;
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

    private readonly Size bufferSize;
    private readonly IntPtr screenDc;
    private readonly IntPtr memDc;
    private readonly IntPtr dibSection;
    private readonly IntPtr oldBitmap;
    private readonly Graphics graphics;
    private readonly MouseTrailRenderer trailRenderer;
    private readonly GestureHintRenderer hintRenderer;
    private readonly LevelOsdRenderer levelOsdRenderer;
    private Rectangle screenBounds;
    private bool presentationFailureReported;

    public event EventHandler? PresentationFailed;

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
        trailRenderer = new MouseTrailRenderer(dpiFactor);
        hintRenderer = new GestureHintRenderer(() => _ = RedrawGestureHint(), ExpireGestureHint);
        levelOsdRenderer = new LevelOsdRenderer(() => _ = RedrawLevelOsd(), ExpireLevelOsd);

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
    }

    public void ApplySettings(MouseTrailUiSettings? settings)
    {
        hintRenderer.ResetOpacity();
        trailRenderer.ApplySettings(settings, Math.Max(1f, DeviceDpi / 96f));
        if (Visible)
        {
            RedrawOverlay();
        }
    }

    public void ApplyHintSettings(GestureHintUiSettings? settings)
    {
        hintRenderer.ApplySettings(settings);
        if (Visible)
        {
            RedrawOverlay();
        }
    }

    public void ApplyLevelOsdSettings(LevelOsdUiSettings? settings)
    {
        levelOsdRenderer.ApplySettings(settings);
        if (Visible)
        {
            RedrawOverlay();
            HideOverlayIfEmpty();
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (trailRenderer.SetHighlighted(highlighted))
        {
            RedrawOverlay();
        }
    }

    public void ShowGestureHint(string ruleName, Point anchor, bool autoHide)
    {
        if (IsDisposed)
        {
            return;
        }

        var dirtyRect = hintRenderer.GetBounds(graphics, screenBounds);
        hintRenderer.Show(ruleName, anchor, autoHide);
        dirtyRect = Union(dirtyRect, hintRenderer.GetBounds(graphics, screenBounds));
        if (!Visible)
        {
            Show();
        }

        RedrawOverlay(dirtyRect);
    }

    internal OverlayRenderTiming CompletePathWithHint(string ruleName, Point anchor)
    {
        if (IsDisposed)
        {
            return OverlayRenderTiming.Empty;
        }

        var dirtyRect = trailRenderer.GetBounds(bufferSize);
        dirtyRect = Union(dirtyRect, hintRenderer.GetBounds(graphics, screenBounds));
        trailRenderer.Reset();
        hintRenderer.Show(ruleName, anchor, autoHide: true);
        dirtyRect = Union(dirtyRect, hintRenderer.GetBounds(graphics, screenBounds));
        if (!Visible)
        {
            Show();
        }

        return RedrawOverlay(dirtyRect);
    }

    public void ClearGestureHint()
    {
        if (IsDisposed)
        {
            return;
        }

        var dirtyRect = hintRenderer.GetBounds(graphics, screenBounds);
        if (!hintRenderer.Clear())
        {
            return;
        }

        RedrawOverlay(dirtyRect);
        HideOverlayIfEmpty();
    }

    public void EndPath()
    {
        if (IsDisposed)
        {
            return;
        }

        var dirtyRect = trailRenderer.GetBounds(bufferSize);
        trailRenderer.Reset();
        RedrawOverlay(dirtyRect);
        if (hintRenderer.HasHint)
        {
            hintRenderer.ScheduleAutoHide();
            return;
        }

        HideOverlayIfEmpty();
    }

    internal TrailFrameTiming ShowPath(IReadOnlyList<Point> points, GestureMouseButton button)
    {
        if (IsDisposed || points.Count < 2)
        {
            HideTrail();
            return TrailFrameTiming.Empty;
        }

        var startedAt = Stopwatch.GetTimestamp();
        Rectangle? dirtyRect = null;
        if (hintRenderer.IsTiming)
        {
            dirtyRect = hintRenderer.GetBounds(graphics, screenBounds);
            hintRenderer.Clear();
        }

        if (!Visible)
        {
            Show();
        }
        var preparedAt = Stopwatch.GetTimestamp();

        var isNewPath = !trailRenderer.IsTracking;
        if (!trailRenderer.IsTracking)
        {
            trailRenderer.StartPath();
        }

        var startIndex = isNewPath ? 0 : points.Count - 1;
        for (var index = startIndex; index < points.Count; index++)
        {
            var nextDirtyRect = trailRenderer.AppendPoint(graphics, ToLocalPoint(points[index]), bufferSize);
            if (nextDirtyRect is not null)
            {
                dirtyRect = dirtyRect is null ? nextDirtyRect : Union(dirtyRect.Value, nextDirtyRect.Value);
            }
        }

        var drawnAt = Stopwatch.GetTimestamp();
        var overlayTiming = OverlayRenderTiming.Empty;
        if (dirtyRect is not null)
        {
            overlayTiming = RedrawOverlay(dirtyRect.Value);
        }

        return new TrailFrameTiming(
            Stopwatch.GetElapsedTime(startedAt, preparedAt).TotalMilliseconds,
            Stopwatch.GetElapsedTime(preparedAt, drawnAt).TotalMilliseconds + overlayTiming.DrawMilliseconds,
            overlayTiming.PresentMilliseconds,
            overlayTiming.IsPresented);
    }

    public void HideTrail()
    {
        if (IsDisposed)
        {
            return;
        }

        var dirtyRect = Union(
            trailRenderer.GetBounds(bufferSize),
            hintRenderer.GetBounds(graphics, screenBounds));
        hintRenderer.Clear();
        trailRenderer.Reset();
        RedrawOverlay(dirtyRect);
        HideOverlayIfEmpty();
    }

    internal void ShowLevelOsd(LevelOsdRequest request)
    {
        if (IsDisposed)
        {
            return;
        }

        var dirtyRect = levelOsdRenderer.GetBounds(screenBounds);
        if (!levelOsdRenderer.Show(request))
        {
            HideOverlayIfEmpty();
            return;
        }

        dirtyRect = Union(dirtyRect, levelOsdRenderer.GetBounds(screenBounds));

        if (!Visible)
        {
            Show();
        }

        RedrawOverlay(dirtyRect);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hintRenderer.Dispose();
            levelOsdRenderer.Dispose();
            trailRenderer.Dispose();
            graphics.Dispose();
            SelectObject(memDc, oldBitmap);
            DeleteObject(dibSection);
            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }

        base.Dispose(disposing);
    }

    private OverlayRenderTiming RedrawOverlay(Rectangle? dirtyRect = null)
    {
        if (IsDisposed)
        {
            return OverlayRenderTiming.Empty;
        }

        var startedAt = Stopwatch.GetTimestamp();
        if (dirtyRect is { } dirty)
        {
            dirty.Intersect(new Rectangle(Point.Empty, bufferSize));
            if (dirty.Width <= 0 || dirty.Height <= 0)
            {
                return OverlayRenderTiming.Empty;
            }

            var previousMode = graphics.CompositingMode;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            using (var clearBrush = new SolidBrush(Color.Transparent))
            {
                graphics.FillRectangle(clearBrush, dirty);
            }

            graphics.CompositingMode = previousMode;
            var state = graphics.Save();
            graphics.SetClip(dirty);
            trailRenderer.Draw(graphics);
            hintRenderer.Draw(graphics, screenBounds);
            levelOsdRenderer.Draw(graphics, screenBounds);
            graphics.Restore(state);
            var drawnAt = Stopwatch.GetTimestamp();
            var isPresented = Present(dirty);
            var presentedAt = Stopwatch.GetTimestamp();
            return new OverlayRenderTiming(
                Stopwatch.GetElapsedTime(startedAt, drawnAt).TotalMilliseconds,
                Stopwatch.GetElapsedTime(drawnAt, presentedAt).TotalMilliseconds,
                dirty,
                isPresented);
        }

        graphics.Clear(Color.Transparent);
        trailRenderer.Draw(graphics);
        hintRenderer.Draw(graphics, screenBounds);
        levelOsdRenderer.Draw(graphics, screenBounds);
        var fullDrawnAt = Stopwatch.GetTimestamp();

        if (Visible)
        {
            var isPresented = Present(new Rectangle(Point.Empty, bufferSize), fullWindow: true);
            return new OverlayRenderTiming(
                Stopwatch.GetElapsedTime(startedAt, fullDrawnAt).TotalMilliseconds,
                Stopwatch.GetElapsedTime(fullDrawnAt).TotalMilliseconds,
                new Rectangle(Point.Empty, bufferSize),
                isPresented);
        }

        return new OverlayRenderTiming(
            Stopwatch.GetElapsedTime(startedAt, fullDrawnAt).TotalMilliseconds,
            Stopwatch.GetElapsedTime(fullDrawnAt).TotalMilliseconds,
            new Rectangle(Point.Empty, bufferSize),
            false);
    }

    private static Rectangle? Union(Rectangle? first, Rectangle? second)
    {
        if (first is null)
        {
            return second;
        }

        if (second is null)
        {
            return first;
        }

        return Rectangle.Union(first.Value, second.Value);
    }

    private bool Present(Rectangle dirtyRect, bool fullWindow = false)
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
            SourceConstantAlpha = 255,
            AlphaFormat = 1
        };

        if (fullWindow)
        {
            var succeeded = UpdateLayeredWindow(
                Handle,
                screenDc,
                ref dstPoint,
                ref size,
                memDc,
                ref srcPoint,
                0,
                ref blend,
                UlwAlpha);
            var errorCode = succeeded ? 0 : Marshal.GetLastWin32Error();
            return ReportPresentationResult(succeeded, errorCode, "UpdateLayeredWindow", dirtyRect);
        }

        return PresentDirty(dirtyRect, dstPoint, size, srcPoint, blend);
    }

    private unsafe bool PresentDirty(
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

        var succeeded = UpdateLayeredWindowIndirect(Handle, ref updateInfo);
        var errorCode = succeeded ? 0 : Marshal.GetLastWin32Error();
        return ReportPresentationResult(succeeded, errorCode, "UpdateLayeredWindowIndirect", dirtyRect);
    }

    private bool ReportPresentationResult(bool succeeded, int errorCode, string operation, Rectangle dirtyRect)
    {
        if (succeeded)
        {
            presentationFailureReported = false;
            return true;
        }

        if (!presentationFailureReported)
        {
            presentationFailureReported = true;
            AppLogger.Warning(
                "MouseTrailForm",
                "layered-window-present-failed",
                $"{operation} failed. Error: {errorCode}; handle: 0x{Handle.ToInt64():X}; visible: {Visible}; cachedScreen: {screenBounds}; currentScreen: {SystemInformation.VirtualScreen}; dirty: {dirtyRect}.");
            PresentationFailed?.Invoke(this, EventArgs.Empty);
        }

        return false;
    }

    private PointF ToLocalPoint(Point point)
    {
        return new PointF(point.X - screenBounds.Left, point.Y - screenBounds.Top);
    }

    private void ExpireGestureHint()
    {
        if (IsDisposed)
        {
            return;
        }

        var dirtyRect = hintRenderer.GetBounds(graphics, screenBounds);
        if (!hintRenderer.Clear())
        {
            return;
        }

        RedrawOverlay(dirtyRect);
        HideOverlayIfEmpty();
    }

    private void ExpireLevelOsd()
    {
        if (IsDisposed)
        {
            return;
        }

        var dirtyRect = levelOsdRenderer.GetBounds(screenBounds);
        if (!levelOsdRenderer.Clear())
        {
            return;
        }

        RedrawOverlay(dirtyRect);
        HideOverlayIfEmpty();
    }

    private OverlayRenderTiming RedrawGestureHint()
    {
        return RedrawOverlay(hintRenderer.GetBounds(graphics, screenBounds));
    }

    private OverlayRenderTiming RedrawLevelOsd()
    {
        return RedrawOverlay(levelOsdRenderer.GetBounds(screenBounds));
    }

    private void HideOverlayIfEmpty()
    {
        if (IsDisposed || trailRenderer.HasPath || hintRenderer.HasHint || levelOsdRenderer.HasOsd)
        {
            return;
        }

        if (Visible)
        {
            Hide();
        }
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

internal readonly record struct OverlayRenderTiming(
    double DrawMilliseconds,
    double PresentMilliseconds,
    Rectangle DirtyRect,
    bool IsPresented)
{
    public static OverlayRenderTiming Empty { get; } = new(0, 0, Rectangle.Empty, false);
}

internal readonly record struct TrailFrameTiming(
    double PrepareMilliseconds,
    double DrawMilliseconds,
    double PresentMilliseconds,
    bool IsPresented)
{
    public static TrailFrameTiming Empty { get; } = new(0, 0, 0, false);
}
