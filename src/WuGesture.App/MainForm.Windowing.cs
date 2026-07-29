using System.Runtime.InteropServices;

namespace WuGesture.App;

public sealed partial class MainForm
{
    private const int SwShow = 5;
    private const int SwMinimize = 6;
    private const int SwRestore = 9;
    private const int WmClose = 0x0010;
    private const int WmNchitTest = 0x0084;
    private const int WmNcLeftButtonDown = 0x00A1;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmWindowCornerPreferenceRound = 2;
    private const int WsMinimizeBox = 0x00020000;
    private const int WsMaximizeBox = 0x00010000;
    private const int WsSysMenu = 0x00080000;
    private const int HtCaption = 0x0002;
    private const int HtLeft = 0x000A;
    private const int HtRight = 0x000B;
    private const int HtTop = 0x000C;
    private const int HtTopLeft = 0x000D;
    private const int HtTopRight = 0x000E;
    private const int HtBottom = 0x000F;
    private const int HtBottomLeft = 0x0010;
    private const int HtBottomRight = 0x0011;
    private const int ResizeBorderThickness = 8;

    protected override void WndProc(ref Message m)
    {
        // Gesture window actions use WM_CLOSE directly, before WinForms assigns a CloseReason.
        if (m.Msg == WmClose && HandleConfiguredUserClose())
        {
            return;
        }

        base.WndProc(ref m);

        if (m.Msg == WmNchitTest && WindowState != FormWindowState.Maximized)
        {
            var resizeHitTest = GetResizeHitTest(PointToClient(Cursor.Position));
            if (resizeHitTest is not null)
            {
                m.Result = (IntPtr)resizeHitTest.Value;
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyWindowCornerPreference();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.Style |= WsMinimizeBox | WsMaximizeBox | WsSysMenu;
            return createParams;
        }
    }

    private void ToggleWindowMaximized()
    {
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
        PostWindowState();
    }

    private void StartWindowDrag()
    {
        ReleaseCapture();
        SendMessage(Handle, WmNcLeftButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
    }

    private void MinimizeWindow()
    {
        ShowWindow(Handle, SwMinimize);
    }

    private void StartWindowResize()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            return;
        }

        PostWindowResizeState(true);
        try
        {
            ReleaseCapture();
            SendMessage(Handle, WmNcLeftButtonDown, (IntPtr)HtBottomRight, IntPtr.Zero);
        }
        finally
        {
            PostWindowResizeState(false);
        }
    }

    private void BringWindowToFront(bool attachToForegroundInputFirst = false)
    {
        if (!IsHandleCreated)
        {
            return;
        }

        if (attachToForegroundInputFirst)
        {
            BringWindowToFrontWithAttachedInput();
            return;
        }

        RestoreAndActivateWindow();
        if (GetForegroundWindow() != Handle)
        {
            BringWindowToFrontWithAttachedInput();
        }
    }

    private void BringWindowToFrontWithAttachedInput()
    {
        var foregroundWindow = GetForegroundWindow();
        var currentThreadId = GetCurrentThreadId();
        var foregroundThreadId = foregroundWindow == IntPtr.Zero
            ? 0
            : GetWindowThreadProcessId(foregroundWindow, out _);
        var isInputAttached = foregroundThreadId != 0 &&
            foregroundThreadId != currentThreadId &&
            AttachThreadInput(currentThreadId, foregroundThreadId, true);

        try
        {
            if (isInputAttached)
            {
                RestoreAndActivateWindow(bringToTop: true);
            }
            else
            {
                RestoreAndActivateWindow();
            }
        }
        finally
        {
            if (isInputAttached)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    private void RestoreAndActivateWindow(bool bringToTop = false)
    {
        if (IsIconic(Handle))
        {
            ShowWindow(Handle, SwRestore);
        }
        else
        {
            ShowWindow(Handle, SwShow);
        }

        if (bringToTop)
        {
            BringWindowToTop(Handle);
        }

        SetForegroundWindow(Handle);
        Activate();
        Focus();
    }

    private void ApplyInitialWindowState()
    {
        if (windowStateStore.TryLoad(out var bounds, out var maximized))
        {
            Bounds = bounds;
            startMaximized = maximized;
            return;
        }

        Bounds = windowStateStore.GetDefaultBounds();
    }

    private void SaveWindowState()
    {
        windowStateStore.Save(WindowState, Bounds, RestoreBounds);
    }

    private int? GetResizeHitTest(Point point)
    {
        var border = Math.Max(1, (int)Math.Ceiling(ResizeBorderThickness * DeviceDpi / 96d));
        var isLeft = point.X <= border;
        var isRight = point.X >= ClientSize.Width - border;
        var isTop = point.Y <= border;
        var isBottom = point.Y >= ClientSize.Height - border;

        if (isTop && isLeft)
        {
            return HtTopLeft;
        }

        if (isTop && isRight)
        {
            return HtTopRight;
        }

        if (isBottom && isLeft)
        {
            return HtBottomLeft;
        }

        if (isBottom && isRight)
        {
            return HtBottomRight;
        }

        if (isLeft)
        {
            return HtLeft;
        }

        if (isRight)
        {
            return HtRight;
        }

        if (isTop)
        {
            return HtTop;
        }

        return isBottom ? HtBottom : null;
    }

    private void ApplyWindowCornerPreference()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var preference = DwmWindowCornerPreferenceRound;
        _ = DwmSetWindowAttribute(
            Handle,
            DwmWindowCornerPreference,
            ref preference,
            Marshal.SizeOf<int>());
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
