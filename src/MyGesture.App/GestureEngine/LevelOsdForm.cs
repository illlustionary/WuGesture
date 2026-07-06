using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MyGesture.App.GestureEngine;

internal sealed class LevelOsdForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int FormWidth = 210;
    private const int FormHeight = 190;
    private const int ShowDurationMs = 1800;
    private const double FadeStep = 0.08d;
    private static readonly object Sync = new();
    private static LevelOsdForm? instance;

    private readonly System.Windows.Forms.Timer hideTimer = new();
    private readonly System.Windows.Forms.Timer fadeTimer = new();
    private readonly Bitmap? volumeIcon;
    private readonly Bitmap? brightnessIcon;
    private OsdKind kind = OsdKind.Volume;
    private int value;
    private bool muted;

    private LevelOsdForm()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Size = new Size(FormWidth, FormHeight);
        BackColor = Color.FromArgb(40, 40, 44);
        Opacity = 1d;

        hideTimer.Interval = ShowDurationMs;
        hideTimer.Tick += OnHideTimerTick;
        fadeTimer.Interval = 20;
        fadeTimer.Tick += OnFadeTimerTick;
        volumeIcon = LoadResourceBitmap("MyGesture.App.Resources.volume.png");
        brightnessIcon = LoadResourceBitmap("MyGesture.App.Resources.sun.png");
        UpdateWindowRegion();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExNoActivate | WsExToolWindow;
            return createParams;
        }
    }

    public static void ShowVolume(int volume, bool isMuted)
    {
        ShowLevel(OsdKind.Volume, volume, isMuted);
    }

    public static void ShowBrightness(int brightness)
    {
        ShowLevel(OsdKind.Brightness, brightness, false);
    }

    private static void ShowLevel(OsdKind kind, int value, bool muted)
    {
        var form = EnsureInstance();
        if (form.InvokeRequired)
        {
            form.BeginInvoke(() => form.ShowInternal(kind, value, muted));
            return;
        }

        form.ShowInternal(kind, value, muted);
    }

    private static LevelOsdForm EnsureInstance()
    {
        lock (Sync)
        {
            if (instance is { IsDisposed: false })
            {
                return instance;
            }

            instance = new LevelOsdForm();
            return instance;
        }
    }

    private void ShowInternal(OsdKind nextKind, int nextValue, bool nextMuted)
    {
        kind = nextKind;
        value = Math.Max(0, Math.Min(100, nextValue));
        muted = nextMuted;

        hideTimer.Stop();
        fadeTimer.Stop();
        Opacity = 1d;
        MoveToCenter();

        if (!Visible)
        {
            Show();
        }

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopmost,
            Left,
            Top,
            Width,
            Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);

        Invalidate();
        Update();
        hideTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var background = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 22);
        using var backgroundBrush = new SolidBrush(Color.FromArgb(225, 40, 40, 44));
        using var borderPen = new Pen(Color.FromArgb(50, 120, 120, 120), 1);
        graphics.FillPath(backgroundBrush, background);
        graphics.DrawPath(borderPen, background);

        var iconBounds = new Rectangle((Width - 56) / 2, 30, 56, 56);
        DrawIcon(graphics, kind == OsdKind.Volume ? volumeIcon : brightnessIcon, iconBounds);

        DrawTrack(graphics);
        DrawText(graphics);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateWindowRegion();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hideTimer.Stop();
            hideTimer.Tick -= OnHideTimerTick;
            hideTimer.Dispose();
            fadeTimer.Stop();
            fadeTimer.Tick -= OnFadeTimerTick;
            fadeTimer.Dispose();
            volumeIcon?.Dispose();
            brightnessIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void DrawTrack(Graphics graphics)
    {
        const int trackWidth = 150;
        const int trackHeight = 10;
        var x = (Width - trackWidth) / 2;
        var y = 108;
        using var trackPath = RoundedRect(new Rectangle(x, y, trackWidth, trackHeight), 5);
        using var trackBrush = new SolidBrush(Color.FromArgb(255, 70, 70, 70));
        graphics.FillPath(trackBrush, trackPath);

        var fillWidth = muted ? 0 : (int)(trackWidth * value / 100d);
        if (fillWidth <= 0)
        {
            return;
        }

        fillWidth = Math.Max(trackHeight, fillWidth);
        using var fillPath = RoundedRect(new Rectangle(x, y, fillWidth, trackHeight), 5);
        var start = kind == OsdKind.Volume ? Color.FromArgb(100, 200, 255) : Color.FromArgb(255, 200, 40);
        var end = kind == OsdKind.Volume ? Color.FromArgb(150, 230, 255) : Color.White;
        using var fillBrush = new LinearGradientBrush(new Rectangle(x, y, fillWidth, trackHeight), start, end, LinearGradientMode.Horizontal);
        graphics.FillPath(fillBrush, fillPath);
    }

    private void DrawText(Graphics graphics)
    {
        var text = muted ? "静音" : $"{value}%";
        using var font = new Font("Segoe UI", 13f, FontStyle.Regular);
        using var brush = new SolidBrush(Color.FromArgb(220, 220, 220));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(text, font, brush, new RectangleF(0, 130, Width, 32), format);
    }

    private static void DrawIcon(Graphics graphics, Bitmap? icon, Rectangle bounds)
    {
        if (icon is null)
        {
            return;
        }

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix();
        attributes.SetColorMatrix(matrix);
        graphics.DrawImage(
            icon,
            bounds,
            0,
            0,
            icon.Width,
            icon.Height,
            GraphicsUnit.Pixel,
            attributes);
    }

    private static Bitmap? LoadResourceBitmap(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        return new Bitmap(stream);
    }

    private void MoveToCenter()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;
    }

    private void UpdateWindowRegion()
    {
        using var path = RoundedRect(new Rectangle(0, 0, Width, Height), 22);
        Region?.Dispose();
        Region = new Region(path);
    }

    private void OnHideTimerTick(object? sender, EventArgs e)
    {
        hideTimer.Stop();
        fadeTimer.Start();
    }

    private void OnFadeTimerTick(object? sender, EventArgs e)
    {
        Opacity -= FadeStep;
        if (Opacity > FadeStep)
        {
            return;
        }

        fadeTimer.Stop();
        Hide();
        Opacity = 1d;
    }

    private static GraphicsPath RoundedRect(Rectangle rect, float radius)
    {
        var path = new GraphicsPath();
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

    private enum OsdKind
    {
        Volume,
        Brightness
    }

    private static class NativeMethods
    {
        public static readonly IntPtr HwndTopmost = new(-1);
        public const uint SwpNoActivate = 0x0010;
        public const uint SwpShowWindow = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);
    }
}
