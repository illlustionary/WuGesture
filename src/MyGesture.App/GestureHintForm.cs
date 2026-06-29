using MyGesture.App.GestureEngine;
using System.Runtime.InteropServices;

namespace MyGesture.App;

public sealed class GestureHintForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    private readonly Label label = new();
    private readonly System.Windows.Forms.Timer hideTimer = new();

    public GestureHintForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(32, 32, 32);
        ForeColor = Color.White;
        Opacity = 0.92;
        Width = 420;
        Height = 56;

        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.Font = new Font("Segoe UI", 12, FontStyle.Regular);
        label.Padding = new Padding(16, 0, 16, 0);
        Controls.Add(label);

        hideTimer.Interval = 750;
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

    public void ShowProgress(IReadOnlyList<GestureDirection> pattern)
    {
        if (IsDisposed)
        {
            return;
        }

        hideTimer.Stop();
        label.Text = pattern.Count == 0
            ? "正在识别手势"
            : $"当前手势: {string.Join(" > ", pattern)}";
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
    }

    public void Complete(IReadOnlyList<GestureDirection> pattern, string? actionName = null)
    {
        if (IsDisposed)
        {
            return;
        }

        label.Text = actionName is null
            ? PatternText(pattern)
            : $"{PatternText(pattern)} -> {actionName}";
        MoveToBottomCenter();

        if (!Visible)
        {
            Show();
        }

        hideTimer.Stop();
        hideTimer.Start();
    }

    private static string PatternText(IReadOnlyList<GestureDirection> pattern)
    {
        return pattern.Count == 0
            ? "未识别到手势"
            : $"当前手势: {string.Join(" > ", pattern)}";
    }

    private void MoveToBottomCenter()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Bottom - Height - 28;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hideTimer.Stop();
            hideTimer.Tick -= OnHideTimerTick;
            hideTimer.Dispose();
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
