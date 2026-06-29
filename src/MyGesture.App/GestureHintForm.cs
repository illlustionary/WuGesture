using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MyGesture.App;

public sealed class GestureHintForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int BottomGap = 140;

    private readonly System.Windows.Forms.Timer hideTimer = new();
    private readonly Font titleFont = new("Segoe UI Semibold", 22, FontStyle.Bold);
    private readonly Brush textBrush = new SolidBrush(Color.White);
    private readonly Brush backgroundBrush = new SolidBrush(Color.FromArgb(226, 18, 24, 31));
    private readonly Pen borderPen = new(Color.FromArgb(90, 255, 255, 255), 1.1f);
    private readonly StringFormat centerFormat = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    private string title = "";
    public GestureHintForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(18, 24, 31);
        ForeColor = Color.White;
        Opacity = 0.96;
        Width = 540;
        Height = 144;
        DoubleBuffered = true;

        hideTimer.Interval = 1100;
        hideTimer.Tick += OnHideTimerTick;
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

    public void ShowResult(string ruleName)
    {
        if (IsDisposed)
        {
            return;
        }

        hideTimer.Stop();
        title = string.IsNullOrWhiteSpace(ruleName) ? "已触发" : ruleName;
        ShowOverlay();

        hideTimer.Start();
    }

    public void ClearResult()
    {
        hideTimer.Stop();
        if (!IsDisposed)
        {
            Hide();
        }
    }

    private void ShowOverlay()
    {
        MoveToBottomCenter();

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
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = ClientRectangle;
        bounds.Inflate(-1, -1);

        using var path = RoundedRect(bounds, 24);
        graphics.FillPath(backgroundBrush, path);
        graphics.DrawPath(borderPen, path);

        var titleRect = new RectangleF(22, 30, Width - 44, 44);
        graphics.DrawString(title, titleFont, textBrush, titleRect, centerFormat);

        // No subtitle or icon, just the rule name.
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
        if (!IsDisposed)
        {
            Hide();
        }
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
