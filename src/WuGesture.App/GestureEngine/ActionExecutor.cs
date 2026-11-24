using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

public sealed class ActionExecutor
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventFExtendedKey = 0x0001;
    private const uint KeyEventFKeyUp = 0x0002;
    private const uint MapVkToVsc = 0;
    private const uint MapVkToVscEx = 4;
    private const int SwMinimize = 6;
    private const int SwMaximize = 3;
    private const int SwRestore = 9;
    private const int MaxHotkeySendAttempts = 3;
    private static readonly TimeSpan HotkeyRetryDelay = TimeSpan.FromMilliseconds(30);
    private const int GwlExStyle = -20;
    private const int WsExTopMost = 0x00000008;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const int WmClose = 0x0010;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const uint GetAncestorRoot = 2;
    private static readonly IntPtr HwndTopMost = new(-1);
    private static readonly IntPtr HwndNoTopMost = new(-2);

    public void Execute(
        GestureRule rule,
        IntPtr targetWindow,
        bool useCurrentWindowWhenTargetMissing = false)
    {
        if (targetWindow == IntPtr.Zero && useCurrentWindowWhenTargetMissing)
        {
            targetWindow = GetForegroundWindow();
        }

        switch (rule.Action)
        {
            case HotkeyAction hotkey:
                ExecuteHotkey(hotkey, targetWindow);
                break;
            case WindowControlAction window:
                ExecuteWindowControl(window, targetWindow, useCurrentWindowWhenTargetMissing);
                break;
            case VolumeControlAction volume:
                ExecuteVolumeControl(volume);
                break;
            case BrightnessControlAction brightness:
                ExecuteBrightnessControl(brightness);
                break;
            case ProgramAction program:
                ExecuteProgram(program);
                break;
        }
    }

    private static void ExecuteHotkey(HotkeyAction action, IntPtr targetWindow)
    {
        if (action.Keys.Count == 0)
        {
            return;
        }

        // Flutter builds its physical-key identity from the scan code. Leaving ScanCode at 0 makes
        // every injected letter share one physical key, so the framework drops the key-down and
        // shortcuts such as Ctrl+C/V do nothing. Translate with the target thread's keyboard
        // layout so keyboard-layout remapping and extended keys are preserved.
        var keyboardLayout = ResolveKeyboardLayout(targetWindow);
        var keyDowns = new Input[action.Keys.Count];
        for (var i = 0; i < action.Keys.Count; i++)
        {
            keyDowns[i] = CreateKeyboardInput(action.Keys[i], keyUp: false, keyboardLayout);
        }

        var keyUps = new Input[action.Keys.Count];
        for (var i = 0; i < action.Keys.Count; i++)
        {
            keyUps[i] = CreateKeyboardInput(action.Keys[action.Keys.Count - 1 - i], keyUp: true, keyboardLayout);
        }

        var batch = new Input[keyDowns.Length + keyUps.Length];
        keyDowns.CopyTo(batch, 0);
        keyUps.CopyTo(batch, keyDowns.Length);

        var keyNames = string.Join("+", action.Keys);
        var sent = 0;
        var lastError = 0;

        // SendInput drops the whole batch or part of it when the input desktop is switching (Task View,
        // secure desktop, lock screen) or when the target window is at a higher integrity level. A short
        // retry covers the transient case; the last error is preserved for the persistent one.
        for (var attempt = 1; attempt <= MaxHotkeySendAttempts; attempt++)
        {
            sent = (int)SendInput((uint)batch.Length, batch, Marshal.SizeOf<Input>());
            if (sent == batch.Length)
            {
                return;
            }

            lastError = Marshal.GetLastWin32Error();

            // A partial insert may have left modifiers logically pressed. Releasing every key again is
            // harmless for keys that never went down and prevents a stuck Win/Alt/Ctrl key.
            _ = SendInput((uint)keyUps.Length, keyUps, Marshal.SizeOf<Input>());

            if (attempt < MaxHotkeySendAttempts)
            {
                AppLogger.Warning(
                    "ActionExecutor",
                    "hotkey-retry",
                    $"SendInput inserted {sent}/{batch.Length} events for {keyNames} (Win32 error {lastError}: {DescribeError(lastError)}); retrying.");
                Thread.Sleep(HotkeyRetryDelay);
            }
        }

        throw new Win32Exception(
            lastError,
            $"Failed to send keyboard input for {keyNames} ({sent}/{batch.Length} events inserted, Win32 error {lastError}: {DescribeError(lastError)}).");
    }

    /// <summary>
    /// 前台激活失败时的降级投递：把单个无修饰键的按键直接投递给目标窗口，不依赖前台状态。
    /// 仅用于 Escape、Enter、方向键这类单键；组合键仍由调用方跳过，避免按键落入错误窗口。
    /// </summary>
    internal static bool TryPostHotkeyToTarget(HotkeyAction action, IntPtr targetWindow)
    {
        if (!HotkeyDeliveryPolicy.CanPostToTarget(action) ||
            targetWindow == IntPtr.Zero ||
            !IsWindow(targetWindow))
        {
            return false;
        }

        var key = action.Keys[0];
        var scanCode = ResolveScanCode(key, ResolveKeyboardLayout(targetWindow));
        var messageWindow = ResolveKeyMessageWindow(targetWindow);
        var virtualKey = (IntPtr)(int)key;
        var keyDown = new IntPtr(HotkeyDeliveryPolicy.BuildKeyMessageLParam(
            scanCode.Code,
            scanCode.IsExtended,
            keyUp: false));
        if (!PostMessage(messageWindow, WmKeyDown, virtualKey, keyDown))
        {
            return false;
        }

        // The key-down already reached the target; a failed key-up must not turn the whole
        // delivery into "skipped", otherwise diagnostics would hide the partial delivery.
        var keyUp = new IntPtr(HotkeyDeliveryPolicy.BuildKeyMessageLParam(
            scanCode.Code,
            scanCode.IsExtended,
            keyUp: true));
        _ = PostMessage(messageWindow, WmKeyUp, virtualKey, keyUp);
        return true;
    }

    /// <summary>
    /// 键盘消息应投递给目标窗口线程的焦点窗口（例如 Win32 应用的输入框），只有焦点窗口
    /// 属于目标顶层窗口时才使用它，避免把按键投到同进程的其他窗口。
    /// </summary>
    private static IntPtr ResolveKeyMessageWindow(IntPtr targetWindow)
    {
        var threadId = GetWindowThreadProcessId(targetWindow, out _);
        if (threadId == 0)
        {
            return targetWindow;
        }

        var info = new GuiThreadInfo { Size = Marshal.SizeOf<GuiThreadInfo>() };
        if (!GetGUIThreadInfo(threadId, ref info) || info.FocusWindow == IntPtr.Zero)
        {
            return targetWindow;
        }

        return GetAncestor(info.FocusWindow, GetAncestorRoot) == targetWindow
            ? info.FocusWindow
            : targetWindow;
    }

    private static string DescribeError(int errorCode)
    {
        return errorCode == 0
            ? "no error reported"
            : new Win32Exception(errorCode).Message;
    }

    private static void ExecuteWindowControl(
        WindowControlAction action,
        IntPtr targetWindow,
        bool useCurrentWindowWhenTargetMissing)
    {
        if (targetWindow == IntPtr.Zero && useCurrentWindowWhenTargetMissing)
        {
            targetWindow = GetForegroundWindow();
        }

        if (targetWindow == IntPtr.Zero || !IsWindow(targetWindow))
        {
            return;
        }

        if (!WindowControlTargetPolicy.IsWindowControlTarget(targetWindow))
        {
            return;
        }

        switch (action.Operation)
        {
            case WindowControlOperation.ToggleTopMost:
                ToggleTopMost(targetWindow);
                break;
            case WindowControlOperation.ToggleMaximize:
                ShowWindow(targetWindow, IsZoomed(targetWindow) ? SwRestore : SwMaximize);
                break;
            case WindowControlOperation.Minimize:
                ShowWindow(targetWindow, SwMinimize);
                break;
            case WindowControlOperation.Close:
                PostMessage(targetWindow, WmClose, IntPtr.Zero, IntPtr.Zero);
                break;
        }
    }

    private static void ExecuteVolumeControl(VolumeControlAction action)
    {
        if (!AudioController.IsAvailable)
        {
            var key = action.Operation switch
            {
                VolumeControlOperation.Increase => Keys.VolumeUp,
                VolumeControlOperation.Decrease => Keys.VolumeDown,
                VolumeControlOperation.Mute => Keys.VolumeMute,
                _ => Keys.None
            };

            if (key != Keys.None)
            {
                ExecuteHotkey(new HotkeyAction([key]), IntPtr.Zero);
            }

            return;
        }

        if (action.Operation == VolumeControlOperation.Mute)
        {
            AudioController.SetMute(!AudioController.IsMuted);
        }
        else
        {
            if (AudioController.IsMuted)
            {
                AudioController.SetMute(false);
            }

            var current = AudioController.GetMasterVolume();
            var delta = Math.Max(1, action.Amount) / 100f;
            var next = action.Operation == VolumeControlOperation.Increase
                ? current + delta
                : current - delta;
            AudioController.SetMasterVolume(next);
        }

        var volume = (int)Math.Round(AudioController.GetMasterVolume() * 100);
        LevelOsdOverlay.ShowVolume(volume, AudioController.IsMuted);
    }

    private static void ExecuteBrightnessControl(BrightnessControlAction action)
    {
        BrightnessAdjustmentQueue.Enqueue(action);
    }

    private static void ExecuteProgram(ProgramAction action)
    {
        var path = action.Path.Trim();
        if (path.Length == 0)
        {
            return;
        }

        // Shell-opened documents, shortcuts and folders may not hand back a process handle, so a
        // null result means "the shell handled it", not "the launch failed".
        using var process = Process.Start(CreateProgramStartInfo(action));
    }

    internal static ProcessStartInfo CreateProgramStartInfo(ProgramAction action)
    {
        var path = action.Path.Trim();
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            // Executables keep the direct launch path: ArgumentList gives literal, quote-free argv
            // semantics that existing rules rely on. Everything else (documents, folders, .lnk,
            // .bat, .ps1, URLs) is handed to the shell so the registered handler decides what runs.
            UseShellExecute = !IsDirectlyExecutable(path),
            WorkingDirectory = Path.GetDirectoryName(path) ?? ""
        };

        if (startInfo.UseShellExecute)
        {
            // ShellExecute receives one parameter string, so arguments have to be quoted here
            // instead of going through ArgumentList.
            if (action.Arguments.Count > 0)
            {
                startInfo.Arguments = string.Join(" ", action.Arguments.Select(QuoteShellArgument));
            }

            return startInfo;
        }

        foreach (var argument in action.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static bool IsDirectlyExecutable(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".com", StringComparison.OrdinalIgnoreCase);
    }

    private static string QuoteShellArgument(string argument)
    {
        return argument.Length > 0 && !argument.Any(char.IsWhiteSpace) && !argument.Contains('"')
            ? argument
            : "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static void ToggleTopMost(IntPtr targetWindow)
    {
        var style = GetWindowLong(targetWindow, GwlExStyle);
        var isTopMost = (style & WsExTopMost) != 0;
        SetWindowPos(
            targetWindow,
            isTopMost ? HwndNoTopMost : HwndTopMost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    private static Input CreateKeyboardInput(Keys key, bool keyUp, IntPtr keyboardLayout)
    {
        var scanCode = ResolveScanCode(key, keyboardLayout);
        var flags = scanCode.IsExtended ? KeyEventFExtendedKey : 0;
        if (keyUp)
        {
            flags |= KeyEventFKeyUp;
        }

        return new Input
        {
            Type = InputKeyboard,
            Union = new InputUnion
            {
                KeyboardInput = new KeyboardInput
                {
                    VirtualKey = (ushort)key,
                    ScanCode = scanCode.Code,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    internal static IntPtr ResolveKeyboardLayout(IntPtr targetWindow)
    {
        if (targetWindow != IntPtr.Zero)
        {
            var threadId = GetWindowThreadProcessId(targetWindow, out _);
            if (threadId != 0)
            {
                var layout = GetKeyboardLayout(threadId);
                if (layout != IntPtr.Zero)
                {
                    return layout;
                }
            }
        }

        return GetKeyboardLayout(0);
    }

    internal static (ushort Code, bool IsExtended) ResolveScanCode(Keys key, IntPtr keyboardLayout)
    {
        var extended = MapVirtualKeyEx((uint)key, MapVkToVscEx, keyboardLayout);
        if (extended != 0)
        {
            return InterpretScanCode(extended, IsExtendedKey(key));
        }

        // Some virtual keys (notably ones without a left/right distinction) only translate with
        // the plain VK-to-VSC mode; keep the known extended-key table as the fallback signal.
        var basic = MapVirtualKeyEx((uint)key, MapVkToVsc, keyboardLayout);
        return InterpretScanCode(basic, IsExtendedKey(key));
    }

    internal static (ushort Code, bool IsExtended) InterpretScanCode(uint mapResult, bool knownExtended)
    {
        // MAPVK_VK_TO_VSC_EX prefixes extended scan codes with 0xE0 or 0xE1 in the high byte.
        var isExtended = (mapResult & 0xFF00) != 0 || knownExtended;
        return ((ushort)(mapResult & 0xFF), isExtended);
    }

    private static bool IsExtendedKey(Keys key)
    {
        return key is
            Keys.Insert or
            Keys.Delete or
            Keys.Home or
            Keys.End or
            Keys.PageUp or
            Keys.PageDown or
            Keys.Up or
            Keys.Down or
            Keys.Left or
            Keys.Right or
            Keys.LWin or
            Keys.RWin or
            Keys.Apps or
            Keys.NumLock or
            Keys.PrintScreen or
            Keys.RMenu or
            Keys.RControlKey;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [DllImport("user32.dll", EntryPoint = "MapVirtualKeyExW")]
    private static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo info);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput MouseInput;

        [FieldOffset(0)]
        public KeyboardInput KeyboardInput;

        [FieldOffset(0)]
        public HardwareInput HardwareInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size;
        public uint Flags;
        public IntPtr ActiveWindow;
        public IntPtr FocusWindow;
        public IntPtr CaptureWindow;
        public IntPtr MenuOwnerWindow;
        public IntPtr MoveSizeWindow;
        public IntPtr CaretWindow;
        public CaretRect CaretRectangle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CaretRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParamLow;
        public ushort ParamHigh;
    }
}
