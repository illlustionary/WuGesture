using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

internal sealed class LevelOsdForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private static readonly object Sync = new();
    private static readonly ManualResetEventSlim Ready = new();
    private static Thread? uiThread;
    private static LevelOsdForm? instance;
    private static LevelOsdUiSettings pendingSettings = new();

    private readonly System.Windows.Forms.Timer hideTimer = new();
    private readonly System.Windows.Forms.Timer fadeTimer = new();
    private readonly Bitmap? volumeIcon;
    private readonly Bitmap? brightnessIcon;
    private LevelOsdUiSettings uiSettings;
    private double fadeStep = 0.08d;
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
        BackColor = Color.FromArgb(40, 40, 44);
        Opacity = 1d;

        hideTimer.Tick += OnHideTimerTick;
        fadeTimer.Interval = 20;
        fadeTimer.Tick += OnFadeTimerTick;
        volumeIcon = LoadResourceBitmap(ResourceNames.VolumeIcon);
        brightnessIcon = LoadResourceBitmap(ResourceNames.BrightnessIcon);
        uiSettings = GetPendingSettings();
        ApplySettingsInternal(uiSettings);
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

    public static void ShowVolumePreview(int volume)
    {
        ShowLevel(OsdKind.Volume, volume, false, true);
    }

    public static void ShowBrightnessPreview(int brightness)
    {
        ShowLevel(OsdKind.Brightness, brightness, false, true);
    }

    public static void ApplySettings(LevelOsdUiSettings settings)
    {
        var nextSettings = NormalizeSettings(settings);
        LevelOsdForm? form;
        lock (Sync)
        {
            pendingSettings = nextSettings;
            form = instance is { IsDisposed: false } ? instance : null;
        }

        if (form is null)
        {
            return;
        }

        if (form.InvokeRequired)
        {
            form.BeginInvoke(() => form.ApplySettingsInternal(nextSettings));
            return;
        }

        form.ApplySettingsInternal(nextSettings);
    }

    private static void ShowLevel(OsdKind kind, int value, bool muted, bool forceShow = false)
    {
        var form = EnsureInstance();
        if (form.InvokeRequired)
        {
            form.BeginInvoke(() => form.ShowInternal(kind, value, muted, forceShow));
            return;
        }

        form.ShowInternal(kind, value, muted, forceShow);
    }

    private static LevelOsdForm EnsureInstance()
    {
        lock (Sync)
        {
            if (instance is { IsDisposed: false })
            {
                return instance;
            }

            Ready.Reset();
            if (uiThread is null || !uiThread.IsAlive)
            {
                uiThread = new Thread(() =>
                {
                    instance = new LevelOsdForm();
                    _ = instance.Handle;
                    Ready.Set();
                    Application.Run();
                });
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.IsBackground = true;
                uiThread.Start();
            }
        }

        Ready.Wait(TimeSpan.FromSeconds(3));
        return instance!;
    }

    private void ShowInternal(OsdKind nextKind, int nextValue, bool nextMuted, bool forceShow)
    {
        if (!forceShow && !IsFeatureEnabled(uiSettings.Enabled))
        {
            Hide();
            return;
        }

        kind = nextKind;
        value = Math.Max(0, Math.Min(100, nextValue));
        muted = nextMuted;

        hideTimer.Stop();
        fadeTimer.Stop();
        Opacity = 1d;
        MoveToPosition();

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

        using var background = RoundedRect(
            new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)),
            GetCornerRadius());
        using var backgroundBrush = new SolidBrush(GetBackgroundColor());
        using var borderPen = new Pen(Color.FromArgb(50, 120, 120, 120), 1);
        graphics.FillPath(backgroundBrush, background);
        graphics.DrawPath(borderPen, background);

        var layout = GetVisualLayout();
        var iconBounds = layout.IconBounds;
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
        var layout = GetVisualLayout();
        var trackWidth = layout.TrackWidth;
        var trackHeight = layout.TrackHeight;
        var x = (Width - trackWidth) / 2;
        var y = layout.TrackY;
        using var trackPath = RoundedRect(new Rectangle(x, y, trackWidth, trackHeight), trackHeight / 2f);
        using var trackBrush = new SolidBrush(GetTrackColor());
        graphics.FillPath(trackBrush, trackPath);

        var fillWidth = muted ? 0 : (int)(trackWidth * value / 100d);
        if (fillWidth <= 0)
        {
            return;
        }

        fillWidth = Math.Max(trackHeight, fillWidth);
        using var fillPath = RoundedRect(new Rectangle(x, y, fillWidth, trackHeight), trackHeight / 2f);
        var start = GetAccentColor();
        var end = Lighten(start);
        using var fillBrush = new LinearGradientBrush(new Rectangle(x, y, fillWidth, trackHeight), start, end, LinearGradientMode.Horizontal);
        graphics.FillPath(fillBrush, fillPath);
    }

    private void DrawText(Graphics graphics)
    {
        var text = muted ? "静音" : $"{value}%";
        var layout = GetVisualLayout();
        var scale = Math.Min(Width / 210d, Height / 190d);
        using var font = new Font("Segoe UI", Math.Max(9f, (float)(13 * scale)), FontStyle.Regular);
        using var brush = new SolidBrush(GetTextColor());
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(text, font, brush, layout.TextBounds, format);
    }

    private (Rectangle IconBounds, int TrackWidth, int TrackHeight, int TrackY, RectangleF TextBounds) GetVisualLayout()
    {
        var scale = Math.Min(Width / 210d, Height / 190d);
        var iconSize = Math.Max(
            24,
            Math.Min(
                56,
                Math.Min(Math.Max(24, Width - 32), (int)Math.Round(Height * 0.3d))));
        var iconTop = Math.Max(8, (int)Math.Round(Height * 0.12d));
        var trackHeight = Math.Max(5, (int)Math.Round(10 * scale));
        var trackWidth = Math.Max(60, Math.Min(Math.Max(60, Width - 36), (int)Math.Round(150 * scale)));
        var minimumTrackY = iconTop + iconSize + 8;
        var preferredTrackY = (int)Math.Round(Height * 0.57d);
        var maximumTrackY = Math.Max(minimumTrackY, Height - trackHeight - 34);
        var trackY = Math.Min(Math.Max(minimumTrackY, preferredTrackY), maximumTrackY);
        var textTop = Math.Min(
            Math.Max(trackY + trackHeight + 8, (int)Math.Round(Height * 0.68d)),
            Math.Max(0, Height - 24));
        var textHeight = Math.Max(20, Height - textTop - 6);

        return (
            new Rectangle((Width - iconSize) / 2, iconTop, iconSize, iconSize),
            trackWidth,
            trackHeight,
            trackY,
            new RectangleF(0, textTop, Width, textHeight));
    }

    private Color GetBackgroundColor()
    {
        var color = GestureColorParser.Parse(uiSettings.BackgroundColor, Color.FromArgb(40, 40, 44));
        var opacity = Math.Clamp(uiSettings.BackgroundOpacity, 0, 100) / 100d;
        return Color.FromArgb(
            (int)Math.Round(255 * opacity),
            color.R,
            color.G,
            color.B);
    }

    private Color GetTextColor()
    {
        return GestureColorParser.Parse(uiSettings.TextColor, Color.FromArgb(220, 220, 220));
    }

    private Color GetTrackColor()
    {
        return GestureColorParser.Parse(uiSettings.TrackColor, Color.FromArgb(70, 70, 70));
    }

    private Color GetAccentColor()
    {
        var fallback = kind == OsdKind.Volume
            ? Color.FromArgb(100, 200, 255)
            : Color.FromArgb(255, 200, 40);
        var value = kind == OsdKind.Volume
            ? uiSettings.VolumeColor
            : uiSettings.BrightnessColor;
        return GestureColorParser.Parse(value, fallback);
    }

    private static Color Lighten(Color color)
    {
        return Color.FromArgb(
            color.A,
            color.R + (255 - color.R) / 3,
            color.G + (255 - color.G) / 3,
            color.B + (255 - color.B) / 3);
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

    private void MoveToPosition()
    {
        var area = GetTargetWorkingArea();
        var x = area.Left + (area.Width - Width) / 2;
        var y = area.Top + (area.Height - Height) / 2;

        switch (uiSettings.Position)
        {
            case GestureConfigContract.LevelOsdPositions.TopCenter:
                x = area.Left + (area.Width - Width) / 2;
                y = area.Top;
                break;
            case GestureConfigContract.LevelOsdPositions.BottomCenter:
                x = area.Left + (area.Width - Width) / 2;
                y = area.Bottom - Height;
                break;
            case GestureConfigContract.LevelOsdPositions.TopLeft:
                x = area.Left;
                y = area.Top;
                break;
            case GestureConfigContract.LevelOsdPositions.TopRight:
                x = area.Right - Width;
                y = area.Top;
                break;
            case GestureConfigContract.LevelOsdPositions.BottomLeft:
                x = area.Left;
                y = area.Bottom - Height;
                break;
            case GestureConfigContract.LevelOsdPositions.BottomRight:
                x = area.Right - Width;
                y = area.Bottom - Height;
                break;
        }

        var maxX = Math.Max(area.Left, area.Right - Width);
        var maxY = Math.Max(area.Top, area.Bottom - Height);
        Left = Math.Clamp(x + uiSettings.OffsetX, area.Left, maxX);
        Top = Math.Clamp(y + uiSettings.OffsetY, area.Top, maxY);
    }

    private static Rectangle GetTargetWorkingArea()
    {
        try
        {
            return Screen.FromPoint(Cursor.Position).WorkingArea;
        }
        catch
        {
            return Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        }
    }

    private void UpdateWindowRegion()
    {
        using var path = RoundedRect(new Rectangle(0, 0, Width, Height), GetCornerRadius());
        Region?.Dispose();
        Region = new Region(path);
    }

    private void OnHideTimerTick(object? sender, EventArgs e)
    {
        hideTimer.Stop();
        if (uiSettings.FadeDurationMs <= 0)
        {
            Hide();
            Opacity = 1d;
            return;
        }

        fadeTimer.Start();
    }

    private void OnFadeTimerTick(object? sender, EventArgs e)
    {
        Opacity -= fadeStep;
        if (Opacity > fadeStep)
        {
            return;
        }

        fadeTimer.Stop();
        Hide();
        Opacity = 1d;
    }

    private void ApplySettingsInternal(LevelOsdUiSettings settings)
    {
        uiSettings = NormalizeSettings(settings);
        Size = new Size(uiSettings.Width, uiSettings.Height);
        hideTimer.Interval = uiSettings.DisplayDurationMs;
        fadeStep = uiSettings.FadeDurationMs <= 0
            ? 1d
            : Math.Min(1d, 20d / uiSettings.FadeDurationMs);
        UpdateWindowRegion();

        if (!IsFeatureEnabled(uiSettings.Enabled))
        {
            hideTimer.Stop();
            fadeTimer.Stop();
            Hide();
            Opacity = 1d;
            return;
        }

        if (Visible)
        {
            MoveToPosition();
            Invalidate();
        }
    }

    private float GetCornerRadius()
    {
        return Math.Min(uiSettings.CornerRadius, Math.Min(Width, Height) / 2f);
    }

    private static LevelOsdUiSettings GetPendingSettings()
    {
        lock (Sync)
        {
            return NormalizeSettings(pendingSettings);
        }
    }

    private static LevelOsdUiSettings NormalizeSettings(LevelOsdUiSettings? settings)
    {
        settings ??= new LevelOsdUiSettings();
        var width = Math.Clamp(settings.Width, 120, 480);
        var height = Math.Clamp(settings.Height, 100, 420);

        return new LevelOsdUiSettings
        {
            Enabled = settings.Enabled ?? true,
            DisplayDurationMs = Math.Clamp(settings.DisplayDurationMs, 300, 5000),
            FadeDurationMs = Math.Clamp(settings.FadeDurationMs, 0, 1000),
            BackgroundColor = NormalizeColor(settings.BackgroundColor, "#28282C"),
            BackgroundOpacity = Math.Clamp(settings.BackgroundOpacity, 0, 100),
            TextColor = NormalizeColor(settings.TextColor, "#DCDCDC"),
            TrackColor = NormalizeColor(settings.TrackColor, "#464646"),
            VolumeColor = NormalizeColor(settings.VolumeColor, "#64C8FF"),
            BrightnessColor = NormalizeColor(settings.BrightnessColor, "#FFC828"),
            Width = width,
            Height = height,
            CornerRadius = Math.Clamp(settings.CornerRadius, 0, Math.Min(width, height) / 2),
            Position = NormalizePosition(settings.Position),
            OffsetX = Math.Clamp(settings.OffsetX, -2000, 2000),
            OffsetY = Math.Clamp(settings.OffsetY, -2000, 2000)
        };
    }

    private static string NormalizePosition(string? value)
    {
        return value is
            GestureConfigContract.LevelOsdPositions.Center or
            GestureConfigContract.LevelOsdPositions.TopCenter or
            GestureConfigContract.LevelOsdPositions.BottomCenter or
            GestureConfigContract.LevelOsdPositions.TopLeft or
            GestureConfigContract.LevelOsdPositions.TopRight or
            GestureConfigContract.LevelOsdPositions.BottomLeft or
            GestureConfigContract.LevelOsdPositions.BottomRight
            ? value
            : GestureConfigContract.LevelOsdPositions.Center;
    }

    private static string NormalizeColor(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool IsFeatureEnabled(bool? enabled)
    {
        return enabled != false;
    }

    private static GraphicsPath RoundedRect(Rectangle rect, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        radius = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
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
