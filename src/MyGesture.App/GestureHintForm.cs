using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MyGesture.App;

public sealed class GestureHintForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int BottomGap = 140;
    private const float CornerRadius = 28f;
    private const double VisibleOpacity = 0.96;
    private const double FadeStep = 0.08;

    private readonly System.Windows.Forms.Timer hideTimer = new();
    private readonly System.Windows.Forms.Timer fadeTimer = new();
    private readonly Font titleFont = new("Segoe UI Semibold", 22, FontStyle.Bold);
    private readonly Brush textBrush = new SolidBrush(Color.White);
    private readonly Brush backgroundBrush = new SolidBrush(Color.FromArgb(226, 18, 24, 31));
    private readonly Pen borderPen = new(Color.FromArgb(90, 255, 255, 255), 1.1f);
    private readonly StringFormat centerFormat = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };

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
        Opacity = VisibleOpacity;
        Width = 540;
        Height = 120;
        DoubleBuffered = true;

        hideTimer.Interval = 1100;
        hideTimer.Tick += OnHideTimerTick;
        fadeTimer.Interval = 24;
        fadeTimer.Tick += OnFadeTimerTick;

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
        Opacity = VisibleOpacity;
        title = string.IsNullOrWhiteSpace(ruleName) ? "已触发" : ruleName;
        ShowOverlay();

        if (autoHide)
        {
            BeginFadeOut();
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
        Opacity = VisibleOpacity;
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
        Opacity = VisibleOpacity;
    }

    private void ShowOverlay()
    {
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

        Invalidate();
        Update();

        if (wasHidden)
        {
            Opacity = VisibleOpacity;
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

        using var path = RoundedRect(bounds, CornerRadius);
        graphics.FillPath(backgroundBrush, path);
        graphics.DrawPath(borderPen, path);

        var titleRect = new RectangleF(28, 0, Width - 56, Height);
        graphics.DrawString(title, titleFont, textBrush, titleRect, centerFormat);
    }

    private void MoveToBottomCenter()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Bottom - Height - BottomGap;
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
            titleFont.Dispose();
            textBrush.Dispose();
            backgroundBrush.Dispose();
            borderPen.Dispose();
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
        Opacity = VisibleOpacity;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateWindowRegion();
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

        using var path = RoundedRect(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region?.Dispose();
        Region = new Region(path);
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
