using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

public sealed class GestureHintForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int WmDisplayChange = 0x007E;
    private const int WmSettingChange = 0x001A;
    private const int WmDwmCompositionChanged = 0x031E;
    private const int HorizontalPadding = 28;
    private const int MinimumWidth = 240;
    private const double FadeStep = 0.08;

    private readonly System.Windows.Forms.Timer hideTimer = new();
    private readonly System.Windows.Forms.Timer fadeTimer = new();
    private readonly StringFormat centerFormat = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };

    private Font? titleFont;
    private Brush? textBrush;
    private Brush? backgroundBrush;
    private Pen? borderPen;
    private GestureHintUiSettings uiSettings = new();
    private string title = "";

    public GestureHintForm()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(18, 24, 31);
        ForeColor = Color.White;
        Opacity = GetTargetOpacity();
        Width = 540;
        Height = 120;
        DoubleBuffered = true;

        hideTimer.Interval = 1100;
        hideTimer.Tick += OnHideTimerTick;
        fadeTimer.Interval = 24;
        fadeTimer.Tick += OnFadeTimerTick;

        ApplySettings(uiSettings);
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

    public void ShowResult(string ruleName, bool autoHide)
    {
        if (IsDisposed)
        {
            return;
        }

        hideTimer.Stop();
        fadeTimer.Stop();
        Opacity = GetTargetOpacity();
        title = string.IsNullOrWhiteSpace(ruleName) ? "已触发" : ruleName;
        UpdateAdaptiveWidth();
        ShowOverlay();

        if (autoHide)
        {
            BeginFadeOut();
        }
    }

    public void ApplySettings(GestureHintUiSettings? settings)
    {
        uiSettings = settings ?? new GestureHintUiSettings();

        DisposeBrushes();

        var baseBackgroundColor = GestureColorParser.Parse(uiSettings.BackgroundColor, Color.FromArgb(18, 24, 31));
        var backgroundColor = Color.FromArgb(255, baseBackgroundColor.R, baseBackgroundColor.G, baseBackgroundColor.B);
        var textColor = GestureColorParser.Parse(uiSettings.TextColor, Color.White);
        titleFont = CreateFont(uiSettings.FontFamily, uiSettings.FontSize);
        textBrush = new SolidBrush(textColor);
        backgroundBrush = new SolidBrush(backgroundColor);
        borderPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1.1f);

        ApplyPercentSize();
        BackColor = Color.FromArgb(backgroundColor.R, backgroundColor.G, backgroundColor.B);
        ForeColor = Color.White;
        Opacity = GetTargetOpacity();

        UpdateWindowRegion();
        UpdateAdaptiveWidth();

        if (IsHandleCreated)
        {
            MoveToBottomCenter();
        }

        if (Visible)
        {
            Invalidate();
            Update();
        }
    }

    public void ClearResult()
    {
        hideTimer.Stop();
        BeginFadeOut();
    }

    public void HideResult()
    {
        hideTimer.Stop();
        fadeTimer.Stop();
        Hide();
        Opacity = GetTargetOpacity();
    }

    public void Preload()
    {
        if (IsDisposed || IsHandleCreated)
        {
            return;
        }

        title = "";
        Opacity = 0;
        MoveToBottomCenter();
        Show();
        Refresh();
        Hide();
        Opacity = GetTargetOpacity();
    }

    private void ShowOverlay()
    {
        RefreshDisplayLayout();
        MoveToBottomCenter();

        var wasHidden = !Visible;
        if (wasHidden)
        {
            Opacity = 0;
        }

        if (!Visible)
        {
            Show();
            Refresh();
        }

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopmost,
            Left,
            Top,
            Width,
            Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);

        Refresh();

        if (wasHidden)
        {
            Opacity = GetTargetOpacity();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = ClientRectangle;
        bounds.Inflate(-1, -1);

        using var path = RoundedRect(bounds, GetCornerRadius());
        graphics.FillPath(backgroundBrush!, path);
        graphics.DrawPath(borderPen!, path);

        var titleRect = new RectangleF(HorizontalPadding, 0, Width - HorizontalPadding * 2, Height);
        using var format = CreateTitleFormat();
        graphics.DrawString(title, titleFont!, textBrush!, titleRect, format);
    }

    private void MoveToBottomCenter()
    {
        var area = GetTargetScreen().WorkingArea;
        var bottomOffset = ResolvePercent(area.Height, uiSettings.BottomOffsetPercent, 0, area.Height);
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Bottom - Height - bottomOffset;
    }

    private void ApplyPercentSize()
    {
        var area = GetTargetScreen().WorkingArea;
        if (!uiSettings.AutoWidth)
        {
            Width = Math.Max(MinimumWidth, ResolvePercent(area.Width, uiSettings.WidthPercent, MinimumWidth, area.Width));
        }

        Height = Math.Max(72, ResolvePercent(area.Height, uiSettings.HeightPercent, 72, area.Height));
    }

    private void UpdateAdaptiveWidth()
    {
        if (!uiSettings.AutoWidth || titleFont is null)
        {
            return;
        }

        var area = GetTargetScreen().WorkingArea;
        var maxWidth = Math.Max(MinimumWidth, area.Width - 24);
        var measuredWidth = MeasureTitleWidth(string.IsNullOrWhiteSpace(title) ? "已触发" : title);
        Width = Math.Min(maxWidth, Math.Max(MinimumWidth, measuredWidth + HorizontalPadding * 2));
        UpdateWindowRegion();
    }

    private int MeasureTitleWidth(string text)
    {
        using var graphics = CreateGraphics();
        using var format = CreateTitleFormat();
        var measuredSize = graphics.MeasureString(text, titleFont!, int.MaxValue, format);
        return (int)Math.Ceiling(measuredSize.Width) + 8;
    }

    private StringFormat CreateTitleFormat()
    {
        return new StringFormat(centerFormat)
        {
            Trimming = uiSettings.AutoWidth ? StringTrimming.None : StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
        };
    }

    private double GetTargetOpacity()
    {
        return Math.Max(0.05d, Math.Min(1d, uiSettings.BackgroundOpacity / 100d));
    }

    private static int ResolvePercent(int size, int percent, int min, int max)
    {
        var value = (int)Math.Round(size * Math.Max(0, percent) / 100d);
        return Math.Max(min, Math.Min(max, value));
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
            DisposeBrushes();
            centerFormat.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnHideTimerTick(object? sender, EventArgs e)
    {
        hideTimer.Stop();
        BeginFadeOut();
    }

    private void OnFadeTimerTick(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        Opacity -= FadeStep;
        if (Opacity > FadeStep)
        {
            return;
        }

        fadeTimer.Stop();
        Hide();
        Opacity = GetTargetOpacity();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateWindowRegion();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg is WmDisplayChange or WmSettingChange or WmDwmCompositionChanged)
        {
            RefreshDisplayLayout();
            if (Visible)
            {
                MoveToBottomCenter();
                Refresh();
            }
        }
    }

    private void BeginFadeOut()
    {
        if (IsDisposed || !Visible)
        {
            return;
        }

        fadeTimer.Stop();
        fadeTimer.Start();
    }

    private void UpdateWindowRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = RoundedRect(new Rectangle(0, 0, Width, Height), GetCornerRadius());
        Region?.Dispose();
        Region = new Region(path);
    }

    private void RefreshDisplayLayout()
    {
        ApplyPercentSize();
        UpdateAdaptiveWidth();
        UpdateWindowRegion();
    }

    private Screen GetTargetScreen()
    {
        return Screen.FromPoint(Cursor.Position);
    }

    private float GetCornerRadius()
    {
        return Math.Max(0f, Math.Min(Math.Min(Width, Height) / 2f, uiSettings.CornerRadius));
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
            return new Font("Segoe UI Semibold", Math.Max(8f, resolvedSize), FontStyle.Bold);
        }
    }

    private void DisposeBrushes()
    {
        titleFont?.Dispose();
        textBrush?.Dispose();
        backgroundBrush?.Dispose();
        borderPen?.Dispose();
        titleFont = null!;
        textBrush = null!;
        backgroundBrush = null!;
        borderPen = null!;
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
