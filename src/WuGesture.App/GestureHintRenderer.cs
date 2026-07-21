using System.Drawing.Drawing2D;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

internal sealed class GestureHintRenderer : IDisposable
{
    private const int FadeFrameIntervalMs = 16;
    private const int HorizontalPadding = 28;
    private const int MinimumWidth = 240;

    private readonly System.Windows.Forms.Timer displayTimer = new();
    private readonly System.Windows.Forms.Timer fadeTimer = new() { Interval = FadeFrameIntervalMs };
    private readonly StringFormat textFormat = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
    };
    private readonly Action requestRedraw;
    private readonly Action requestHide;
    private readonly FadeOverlaySurface fadeSurface = new();
    private GestureHintUiSettings settings = new();
    private Font? font;
    private Color textColor;
    private Color backgroundColor;
    private Color borderColor;
    private string title = "";
    private Point anchor;
    private long fadeStartedAt;

    public GestureHintRenderer(Action requestRedraw, Action requestHide)
    {
        this.requestRedraw = requestRedraw;
        this.requestHide = requestHide;
        displayTimer.Tick += OnDisplayTimerTick;
        fadeTimer.Tick += OnFadeTimerTick;
        ApplySettings(settings);
    }

    public bool HasHint { get; private set; }

    public bool IsTiming => displayTimer.Enabled || fadeTimer.Enabled;

    public byte Opacity { get; private set; } = 255;

    public void ResetOpacity()
    {
        Opacity = 255;
    }

    public void ApplySettings(GestureHintUiSettings? value)
    {
        settings = value ?? new GestureHintUiSettings();

        font?.Dispose();
        var baseBackgroundColor = GestureColorParser.Parse(settings.BackgroundColor, Color.FromArgb(18, 24, 31));
        font = CreateFont(settings.FontFamily, settings.FontSize);
        textColor = GestureColorParser.Parse(settings.TextColor, Color.White);
        backgroundColor = ApplyOpacity(baseBackgroundColor, ClampOpacity(settings.BackgroundOpacity));
        borderColor = Color.FromArgb(90, 255, 255, 255);
    }

    public void Show(string ruleName, Point value, bool autoHide)
    {
        StopTimers();
        Opacity = 255;
        title = string.IsNullOrWhiteSpace(ruleName) ? "已触发" : ruleName;
        anchor = value;
        HasHint = true;

        if (autoHide)
        {
            displayTimer.Interval = Math.Max(1, settings.DisplayDurationMs);
            displayTimer.Start();
        }
    }

    public bool Clear()
    {
        if (!HasHint)
        {
            return false;
        }

        StopTimers();
        HasHint = false;
        Opacity = 255;
        title = "";
        return true;
    }

    public void Draw(Graphics graphics, Rectangle screenBounds)
    {
        if (!HasHint || font is null)
        {
            return;
        }

        var area = Screen.FromPoint(anchor).WorkingArea;
        var maxWidth = Math.Max(MinimumWidth, area.Width - 24);
        var width = settings.AutoWidth
            ? Math.Min(maxWidth, Math.Max(MinimumWidth, MeasureWidth(graphics) + HorizontalPadding * 2))
            : Math.Max(MinimumWidth, ResolvePercent(area.Width, settings.WidthPercent, MinimumWidth, area.Width));
        var height = Math.Max(72, ResolvePercent(area.Height, settings.HeightPercent, 72, area.Height));
        var bottomOffset = ResolvePercent(area.Height, settings.BottomOffsetPercent, 0, area.Height);
        var bounds = new Rectangle(
            area.Left - screenBounds.Left + (area.Width - width) / 2,
            area.Bottom - screenBounds.Top - height - bottomOffset,
            width,
            height);

        var surfaceBounds = Rectangle.Inflate(bounds, 2, 2);
        fadeSurface.Draw(graphics, surfaceBounds, Opacity, surfaceGraphics =>
        {
            using var bubblePath = RoundedRect(bounds, Math.Min(Math.Min(width, height) / 2f, Math.Max(0f, settings.CornerRadius)));
            using var backgroundBrush = new SolidBrush(backgroundColor);
            using var borderPen = new Pen(borderColor, 1.1f);
            surfaceGraphics.FillPath(backgroundBrush, bubblePath);
            surfaceGraphics.DrawPath(borderPen, bubblePath);

            var titleBounds = new RectangleF(
                bounds.Left + HorizontalPadding,
                bounds.Top,
                bounds.Width - HorizontalPadding * 2,
                bounds.Height);
            textFormat.Trimming = settings.AutoWidth ? StringTrimming.None : StringTrimming.EllipsisCharacter;
            using var textBrush = new SolidBrush(textColor);
            surfaceGraphics.DrawString(title, font, textBrush, titleBounds, textFormat);
        });
    }

    public void Dispose()
    {
        StopTimers();
        displayTimer.Tick -= OnDisplayTimerTick;
        displayTimer.Dispose();
        fadeTimer.Tick -= OnFadeTimerTick;
        fadeTimer.Dispose();
        textFormat.Dispose();
        font?.Dispose();
        fadeSurface.Dispose();
    }

    private int MeasureWidth(Graphics graphics)
    {
        var measuredSize = graphics.MeasureString(string.IsNullOrWhiteSpace(title) ? "已触发" : title, font!);
        return (int)Math.Ceiling(measuredSize.Width) + 8;
    }

    private void OnDisplayTimerTick(object? sender, EventArgs e)
    {
        displayTimer.Stop();
        if (!HasHint || settings.FadeDurationMs <= 0)
        {
            requestHide();
            return;
        }

        fadeStartedAt = Environment.TickCount64;
        fadeTimer.Start();
    }

    private void OnFadeTimerTick(object? sender, EventArgs e)
    {
        if (!HasHint)
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

    private static Font CreateFont(string? familyName, float size)
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
}
