using Microsoft.Win32;
using System.Runtime.InteropServices;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

public sealed partial class MainForm
{
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int SwShow = 5;
    private const int SwMinimize = 6;
    private const int SwRestore = 9;

    private void ApplyWindows11TitleBarColors(AppearanceUiSettings appearance)
    {
        if (!IsHandleCreated ||
            !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var useDarkPalette = appearance.Theme == GestureConfigContract.AppearanceThemes.Dark ||
            (appearance.Theme == GestureConfigContract.AppearanceThemes.System && SystemPrefersDarkTheme());
        var captionColor = GestureColorParser.Parse(
            useDarkPalette ? appearance.DarkTitleBarColor : appearance.LightTitleBarColor,
            Color.Empty);
        var textColor = GestureColorParser.Parse(
            useDarkPalette ? appearance.DarkTitleBarTextColor : appearance.LightTitleBarTextColor,
            Color.Empty);
        if (captionColor.IsEmpty || textColor.IsEmpty)
        {
            return;
        }

        var nativeCaptionColor = ColorTranslator.ToWin32(captionColor);
        var nativeTextColor = ColorTranslator.ToWin32(textColor);
        _ = DwmSetWindowAttribute(Handle, DwmwaCaptionColor, ref nativeCaptionColor, sizeof(int));
        _ = DwmSetWindowAttribute(Handle, DwmwaTextColor, ref nativeTextColor, sizeof(int));
    }

    private void OnSystemUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        var uiSettings = loadedConfig?.Config.UiSettings;
        if (uiSettings?.Appearance.Theme != GestureConfigContract.AppearanceThemes.System)
        {
            return;
        }

        BeginInvokeSafe(() => ApplyWindows11TitleBarColors(uiSettings.Appearance));
    }

    private void MinimizeWindow()
    {
        ShowWindow(Handle, SwMinimize);
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

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
