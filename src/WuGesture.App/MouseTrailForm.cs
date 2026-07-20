using System.Drawing.Drawing2D;
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
    private Rectangle screenBounds;

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
        hintRenderer = new GestureHintRenderer(RedrawOverlay, HideTrail);

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
        graphics.Clear(Color.Transparent);
        Present(new Rectangle(Point.Empty, bufferSize), fullWindow: true);
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

        hintRenderer.Show(ruleName, anchor, autoHide);
        if (!Visible)
        {
            Show();
        }

        RedrawOverlay();
    }

    public void ClearGestureHint()
    {
        if (IsDisposed || !hintRenderer.Clear())
        {
            return;
        }

        RedrawOverlay();
        if (!trailRenderer.HasPath)
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

        trailRenderer.Reset();
        if (!hintRenderer.HasHint)
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

        if (hintRenderer.IsTiming)
        {
            hintRenderer.Clear();
            RedrawOverlay();
        }

        var current = ToLocalPoint(points[^1]);
        if (!Visible)
        {
            Show();
            graphics.Clear(Color.Transparent);
            trailRenderer.StartPath();
        }

        var dirtyRect = trailRenderer.AppendPoint(graphics, current, bufferSize);
        if (dirtyRect is not null)
        {
            Present(dirtyRect.Value);
        }
    }

    public void HideTrail()
    {
        if (IsDisposed)
        {
            return;
        }

        hintRenderer.Clear();
        trailRenderer.Reset();
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
            hintRenderer.Dispose();
            trailRenderer.Dispose();
            graphics.Dispose();
            SelectObject(memDc, oldBitmap);
            DeleteObject(dibSection);
            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }

        base.Dispose(disposing);
    }

    private void RedrawOverlay()
    {
        if (IsDisposed)
        {
            return;
        }

        graphics.Clear(Color.Transparent);
        trailRenderer.Draw(graphics);
        hintRenderer.Draw(graphics, screenBounds);

        if (Visible)
        {
            Present(new Rectangle(Point.Empty, bufferSize), fullWindow: true);
        }
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
            SourceConstantAlpha = hintRenderer.Opacity,
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

    private PointF ToLocalPoint(Point point)
    {
        return new PointF(point.X - screenBounds.Left, point.Y - screenBounds.Top);
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
