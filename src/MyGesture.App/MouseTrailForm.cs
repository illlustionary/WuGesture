using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using MyGesture.App.GestureEngine;

namespace MyGesture.App;

public sealed class MouseTrailForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;
    private const int UlwAlpha = 0x00000002;
    private const byte WindowOpacity = 180;

    private Pen inactivePen;
    private Pen activePen;
    private Pen dirtyPen;
    private readonly GraphicsPath path = new();
    private readonly GraphicsPath dirtyPath = new();

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
    private byte windowOpacity = 180;

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

        if (Visible && path.PointCount > 0)
        {
            RedrawPath();
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        RedrawPath();
    }

    public void ShowPath(IReadOnlyList<Point> points, GestureMouseButton button)
    {
        if (IsDisposed || points.Count < 2)
        {
            HideTrail();
            return;
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
            inactivePen.Dispose();
            activePen.Dispose();
            dirtyPen.Dispose();
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

    private void RedrawPath()
    {
        if (IsDisposed || path.PointCount == 0)
        {
            return;
        }

        dirtyPath.Reset();
        dirtyPath.AddPath(path, false);
        dirtyPath.Widen(dirtyPen);

        var dirtyRect = Rectangle.Ceiling(dirtyPath.GetBounds());
        dirtyRect.Intersect(new Rectangle(Point.Empty, bufferSize));

        graphics.SetClip(dirtyPath);
        graphics.Clear(Color.Transparent);
        graphics.ResetClip();
        graphics.DrawPath(isHighlighted ? activePen : inactivePen, path);

        if (Visible)
        {
            Present(dirtyRect);
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
