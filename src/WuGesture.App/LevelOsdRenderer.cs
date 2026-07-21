using System.Drawing.Drawing2D;
using System.Reflection;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

internal sealed class LevelOsdRenderer : IDisposable
{
    private const int FadeFrameIntervalMs = 16;

    private readonly System.Windows.Forms.Timer displayTimer = new();
    private readonly System.Windows.Forms.Timer fadeTimer = new() { Interval = FadeFrameIntervalMs };
    private readonly Action requestRedraw;
    private readonly Action requestHide;
    private readonly FadeOverlaySurface fadeSurface = new();
    private readonly Bitmap? volumeIcon;
    private readonly Bitmap? brightnessIcon;
    private LevelOsdUiSettings settings = new();
    private LevelOsdKind kind = LevelOsdKind.Volume;
    private Point anchor;
    private int value;
    private bool muted;
    private long fadeStartedAt;

    public LevelOsdRenderer(Action requestRedraw, Action requestHide)
    {
        this.requestRedraw = requestRedraw;
        this.requestHide = requestHide;
        volumeIcon = LoadResourceBitmap(ResourceNames.VolumeIcon);
        brightnessIcon = LoadResourceBitmap(ResourceNames.BrightnessIcon);
        displayTimer.Tick += OnDisplayTimerTick;
        fadeTimer.Tick += OnFadeTimerTick;
        ApplySettings(settings);
    }

    public bool HasOsd { get; private set; }

    public byte Opacity { get; private set; } = 255;

    public void ApplySettings(LevelOsdUiSettings? value)
    {
        settings = NormalizeSettings(value);
        if (!IsFeatureEnabled(settings.Enabled))
        {
            Clear();
        }
    }

    public bool Show(LevelOsdRequest request)
    {
        if (!request.ForceShow && !IsFeatureEnabled(settings.Enabled))
        {
            Clear();
            return false;
        }

        StopTimers();
        kind = request.Kind;
        value = Math.Clamp(request.Value, 0, 100);
        muted = request.Muted;
        anchor = Cursor.Position;
        Opacity = 255;
        HasOsd = true;
        displayTimer.Interval = Math.Max(1, settings.DisplayDurationMs);
        displayTimer.Start();
        return true;
    }

    public bool Clear()
    {
        if (!HasOsd)
        {
            return false;
        }

        StopTimers();
        HasOsd = false;
        Opacity = 255;
        return true;
    }

    public void Draw(Graphics graphics, Rectangle screenBounds)
    {
        if (!HasOsd)
        {
            return;
        }

        var bounds = GetBounds(screenBounds);
        var layout = GetVisualLayout(bounds);
        var surfaceBounds = Rectangle.Inflate(bounds, 2, 2);
        fadeSurface.Draw(graphics, surfaceBounds, Opacity, surfaceGraphics =>
        {
            using var backgroundPath = RoundedRect(bounds, settings.CornerRadius);
            using var backgroundBrush = new SolidBrush(WithConfiguredOpacity(
                GestureColorParser.Parse(settings.BackgroundColor, Color.FromArgb(40, 40, 44)),
                settings.BackgroundOpacity));
            using var borderPen = new Pen(WithConfiguredOpacity(Color.FromArgb(50, 120, 120, 120), 100), 1);
            surfaceGraphics.FillPath(backgroundBrush, backgroundPath);
            surfaceGraphics.DrawPath(borderPen, backgroundPath);
            DrawIcon(surfaceGraphics, kind == LevelOsdKind.Volume ? volumeIcon : brightnessIcon, layout.IconBounds);
            DrawTrack(surfaceGraphics, layout);
            DrawText(surfaceGraphics, layout);
        });
    }

    public void Dispose()
    {
        StopTimers();
        displayTimer.Tick -= OnDisplayTimerTick;
        displayTimer.Dispose();
        fadeTimer.Tick -= OnFadeTimerTick;
        fadeTimer.Dispose();
        volumeIcon?.Dispose();
        brightnessIcon?.Dispose();
        fadeSurface.Dispose();
    }

    private Rectangle GetBounds(Rectangle screenBounds)
    {
        var area = Screen.FromPoint(anchor).WorkingArea;
        var width = settings.Width;
        var height = settings.Height;
        var x = area.Left + (area.Width - width) / 2;
        var y = area.Top + (area.Height - height) / 2;

        switch (settings.Position)
        {
            case GestureConfigContract.LevelOsdPositions.TopCenter:
                y = area.Top;
                break;
            case GestureConfigContract.LevelOsdPositions.BottomCenter:
                y = area.Bottom - height;
                break;
            case GestureConfigContract.LevelOsdPositions.TopLeft:
                x = area.Left;
                y = area.Top;
                break;
            case GestureConfigContract.LevelOsdPositions.TopRight:
                x = area.Right - width;
                y = area.Top;
                break;
            case GestureConfigContract.LevelOsdPositions.BottomLeft:
                x = area.Left;
                y = area.Bottom - height;
                break;
            case GestureConfigContract.LevelOsdPositions.BottomRight:
                x = area.Right - width;
                y = area.Bottom - height;
                break;
        }

        var maxX = Math.Max(area.Left, area.Right - width);
        var maxY = Math.Max(area.Top, area.Bottom - height);
        x = Math.Clamp(x + settings.OffsetX, area.Left, maxX);
        y = Math.Clamp(y + settings.OffsetY, area.Top, maxY);
        return new Rectangle(x - screenBounds.Left, y - screenBounds.Top, width, height);
    }

    private (Rectangle IconBounds, Rectangle TrackBounds, RectangleF TextBounds) GetVisualLayout(Rectangle bounds)
    {
        var scale = Math.Min(bounds.Width / 210d, bounds.Height / 190d);
        var iconSize = Math.Max(
            24,
            Math.Min(
                56,
                Math.Min(Math.Max(24, bounds.Width - 32), (int)Math.Round(bounds.Height * 0.3d))));
        var iconTop = bounds.Top + Math.Max(8, (int)Math.Round(bounds.Height * 0.12d));
        var trackHeight = Math.Max(5, (int)Math.Round(10 * scale));
        var trackWidth = Math.Max(60, Math.Min(Math.Max(60, bounds.Width - 36), (int)Math.Round(150 * scale)));
        var minimumTrackY = iconTop + iconSize + 8;
        var preferredTrackY = bounds.Top + (int)Math.Round(bounds.Height * 0.57d);
        var maximumTrackY = Math.Max(minimumTrackY, bounds.Bottom - trackHeight - 34);
        var trackY = Math.Min(Math.Max(minimumTrackY, preferredTrackY), maximumTrackY);
        var textTop = Math.Min(
            Math.Max(trackY + trackHeight + 8, bounds.Top + (int)Math.Round(bounds.Height * 0.68d)),
            Math.Max(bounds.Top, bounds.Bottom - 24));
        var textHeight = Math.Max(20, bounds.Bottom - textTop - 6);

        return (
            new Rectangle(bounds.Left + (bounds.Width - iconSize) / 2, iconTop, iconSize, iconSize),
            new Rectangle(bounds.Left + (bounds.Width - trackWidth) / 2, trackY, trackWidth, trackHeight),
            new RectangleF(bounds.Left, textTop, bounds.Width, textHeight));
    }

    private void DrawTrack(Graphics graphics, (Rectangle IconBounds, Rectangle TrackBounds, RectangleF TextBounds) layout)
    {
        var track = layout.TrackBounds;
        using var trackPath = RoundedRect(track, track.Height / 2f);
        using var trackBrush = new SolidBrush(WithConfiguredOpacity(
            GestureColorParser.Parse(settings.TrackColor, Color.FromArgb(70, 70, 70)),
            100));
        graphics.FillPath(trackBrush, trackPath);

        var fillWidth = muted ? 0 : (int)(track.Width * value / 100d);
        if (fillWidth <= 0)
        {
            return;
        }

        fillWidth = Math.Max(track.Height, fillWidth);
        var fill = new Rectangle(track.Left, track.Top, fillWidth, track.Height);
        using var fillPath = RoundedRect(fill, track.Height / 2f);
        var accent = GestureColorParser.Parse(
            kind == LevelOsdKind.Volume ? settings.VolumeColor : settings.BrightnessColor,
            kind == LevelOsdKind.Volume ? Color.FromArgb(100, 200, 255) : Color.FromArgb(255, 200, 40));
        using var fillBrush = new LinearGradientBrush(
            fill,
            WithConfiguredOpacity(accent, 100),
            WithConfiguredOpacity(Lighten(accent), 100),
            LinearGradientMode.Horizontal);
        graphics.FillPath(fillBrush, fillPath);
    }

    private void DrawText(Graphics graphics, (Rectangle IconBounds, Rectangle TrackBounds, RectangleF TextBounds) layout)
    {
        var scale = Math.Min(settings.Width / 210d, settings.Height / 190d);
        using var font = new Font("Segoe UI", Math.Max(9f, (float)(13 * scale)), FontStyle.Regular);
        using var brush = new SolidBrush(WithConfiguredOpacity(
            GestureColorParser.Parse(settings.TextColor, Color.FromArgb(220, 220, 220)),
            100));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(muted ? "静音" : $"{value}%", font, brush, layout.TextBounds, format);
    }

    private void DrawIcon(Graphics graphics, Bitmap? icon, Rectangle bounds)
    {
        if (icon is null)
        {
            return;
        }

        graphics.DrawImage(icon, bounds);
    }

    private void OnDisplayTimerTick(object? sender, EventArgs e)
    {
        displayTimer.Stop();
        if (!HasOsd || settings.FadeDurationMs <= 0)
        {
            requestHide();
            return;
        }

        fadeStartedAt = Environment.TickCount64;
        fadeTimer.Start();
    }

    private void OnFadeTimerTick(object? sender, EventArgs e)
    {
        if (!HasOsd)
        {
            requestHide();
            return;
        }

        var elapsed = Environment.TickCount64 - fadeStartedAt;
        var progress = Math.Min(1d, elapsed / (double)settings.FadeDurationMs);
        Opacity = (byte)Math.Round(255d * (1d - progress));
        requestRedraw();

        if (progress >= 1d)
        {
            requestHide();
        }
    }

    private void StopTimers()
    {
        displayTimer.Stop();
        fadeTimer.Stop();
    }

    private static Color WithConfiguredOpacity(Color color, int configuredOpacity)
    {
        var configuredAlpha = Math.Clamp(configuredOpacity, 0, 100) / 100d;
        var alpha = (int)Math.Round(color.A * configuredAlpha);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static Color Lighten(Color color)
    {
        return Color.FromArgb(
            color.A,
            color.R + (255 - color.R) / 3,
            color.G + (255 - color.G) / 3,
            color.B + (255 - color.B) / 3);
    }

    private static Bitmap? LoadResourceBitmap(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        return stream is null ? null : new Bitmap(stream);
    }

    private static LevelOsdUiSettings NormalizeSettings(LevelOsdUiSettings? value)
    {
        value ??= new LevelOsdUiSettings();
        var width = Math.Clamp(value.Width, 120, 480);
        var height = Math.Clamp(value.Height, 100, 420);
        return new LevelOsdUiSettings
        {
            Enabled = value.Enabled ?? true,
            DisplayDurationMs = Math.Clamp(value.DisplayDurationMs, 0, 10000),
            FadeDurationMs = Math.Clamp(value.FadeDurationMs, 0, 1000),
            BackgroundColor = value.BackgroundColor,
            BackgroundOpacity = Math.Clamp(value.BackgroundOpacity, 0, 100),
            TextColor = value.TextColor,
            TrackColor = value.TrackColor,
            VolumeColor = value.VolumeColor,
            BrightnessColor = value.BrightnessColor,
            Width = width,
            Height = height,
            CornerRadius = Math.Clamp(value.CornerRadius, 0, Math.Min(width, height) / 2),
            Position = value.Position,
            OffsetX = Math.Clamp(value.OffsetX, -2000, 2000),
            OffsetY = Math.Clamp(value.OffsetY, -2000, 2000)
        };
    }

    private static bool IsFeatureEnabled(bool? enabled) => enabled != false;

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
}
