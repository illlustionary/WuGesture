using System.Runtime.InteropServices;
using Microsoft.Win32;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

public sealed partial class MainForm
{
    private const int SwHide = 0;
    private const int SwShow = 5;
    private const int SwMinimize = 6;
    private const int SwRestore = 9;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTitleBarMetrics();
        if (loadedConfig is not null)
        {
            ApplyWindowTheme(loadedConfig.Config.UiSettings.Appearance);
        }
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyTitleBarMetrics();
        ApplyMinimumWindowSize(e.DeviceDpiNew);
        // The full-screen overlay caches the virtual-screen rectangle and a DIB sized to it, so a scale change
        // leaves it stretched even though the per-monitor metrics above are already up to date.
        ScheduleOverlayRecovery("dpi-changed");
    }

    private void ApplyWindowTheme(AppearanceUiSettings appearance)
    {
        trayMenuController?.ApplyAppearance(appearance);
        ResolveWindowThemeColors(appearance, out var captionColor, out var textColor, out var useDarkPalette);

        // Each colour falls back independently, so one malformed field cannot leave the whole window with the
        // white first-paint background.
        BackColor = captionColor;
        webViewHost.SetInitialAppearance(captionColor, useDarkPalette);
        ApplyTitleBarTheme(captionColor, textColor, useDarkPalette);
    }

    private static void ResolveWindowThemeColors(
        AppearanceUiSettings appearance,
        out Color captionColor,
        out Color textColor,
        out bool useDarkPalette)
    {
        useDarkPalette = appearance.Theme == GestureConfigContract.AppearanceThemes.Dark ||
            (appearance.Theme == GestureConfigContract.AppearanceThemes.System && SystemTheme.PrefersDarkMode());
        var captionFallback = useDarkPalette ? Color.FromArgb(0x1B, 0x22, 0x2B) : Color.FromArgb(0xFF, 0xFF, 0xFF);
        var textFallback = useDarkPalette ? Color.FromArgb(0xED, 0xF2, 0xF7) : Color.FromArgb(0x16, 0x20, 0x2B);
        captionColor = GestureColorParser.Parse(
            useDarkPalette ? appearance.DarkTitleBarColor : appearance.LightTitleBarColor,
            captionFallback);
        textColor = GestureColorParser.Parse(
            useDarkPalette ? appearance.DarkTitleBarTextColor : appearance.LightTitleBarTextColor,
            textFallback);
    }

    private void OnSystemUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        var uiSettings = loadedConfig?.Config.UiSettings;
        if (uiSettings?.Appearance.Theme != GestureConfigContract.AppearanceThemes.System)
        {
            return;
        }

        BeginInvokeSafe(() => ApplyWindowTheme(uiSettings.Appearance));
    }

    private void MinimizeWindow()
    {
        ShowWindow(Handle, SwMinimize);
    }

    /// <summary>
    /// Hides the window even when WinForms already believes it is invisible.
    /// </summary>
    /// <remarks>
    /// The shell can show a hidden window with a native <c>ShowWindow</c> call without going through the
    /// WinForms visibility setter. <see cref="Control.Hide"/> is then a no-op because the managed state is
    /// still false, so the native visibility has to be cleared explicitly.
    /// </remarks>
    private void ForceHideWindow()
    {
        Hide();
        if (IsHandleCreated)
        {
            ShowWindow(Handle, SwHide);
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

    private void ApplyInitialWindowState(int dpi)
    {
        if (windowStateStore.TryLoad(dpi, out var bounds, out var maximized))
        {
            Bounds = bounds;
            startMaximized = maximized;
            return;
        }

        Bounds = WindowStateStore.GetDefaultBounds(dpi, WindowStateStore.GetPrimaryWorkingArea());
    }

    private void ApplyMinimumWindowSize(int dpi)
    {
        var workingArea = IsHandleCreated
            ? Screen.FromControl(this)?.WorkingArea ?? WindowStateStore.GetPrimaryWorkingArea()
            : WindowStateStore.GetPrimaryWorkingArea();
        MinimumSize = WindowStateStore.GetMinimumSize(dpi, workingArea);
    }

    private int GetInitialDpi()
    {
        if (IsHandleCreated && DeviceDpi > 0)
        {
            return DeviceDpi;
        }

        var dpi = GetDpiForSystem();
        return dpi > 0 ? dpi : 96;
    }

    private void SaveWindowState()
    {
        windowStateStore.Save(WindowState, Bounds, RestoreBounds);
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern int GetDpiForSystem();

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
}
