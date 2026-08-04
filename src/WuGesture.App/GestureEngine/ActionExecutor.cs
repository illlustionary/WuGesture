using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

public sealed class ActionExecutor
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventFExtendedKey = 0x0001;
    private const uint KeyEventFKeyUp = 0x0002;
    private const int SwMinimize = 6;
    private const int SwMaximize = 3;
    private const int SwRestore = 9;
    private const int GwlExStyle = -20;
    private const int WsExTopMost = 0x00000008;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const int WmClose = 0x0010;
    private static readonly IntPtr HwndTopMost = new(-1);
    private static readonly IntPtr HwndNoTopMost = new(-2);

    public void Execute(
        GestureRule rule,
        IntPtr targetWindow,
        bool useCurrentWindowWhenTargetMissing = false)
    {
        switch (rule.Action)
        {
            case HotkeyAction hotkey:
                ExecuteHotkey(hotkey);
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

    private static void ExecuteHotkey(HotkeyAction action)
    {
        if (action.Keys.Count == 0)
        {
            return;
        }

        var inputs = new List<Input>(action.Keys.Count * 2);
        foreach (var key in action.Keys)
        {
            inputs.Add(CreateKeyboardInput(key, keyUp: false));
        }

        for (var i = action.Keys.Count - 1; i >= 0; i--)
        {
            inputs.Add(CreateKeyboardInput(action.Keys[i], keyUp: true));
        }

        var inputArray = inputs.ToArray();
        var sent = SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf<Input>());
        if (sent != inputs.Count)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to send keyboard input.");
        }
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

        if (DesktopWindowClassifier.IsDesktopSurface(targetWindow))
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
                ExecuteHotkey(new HotkeyAction([key]));
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

        var startInfo = CreateProgramStartInfo(action);
        _ = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start program.");
    }

    internal static ProcessStartInfo CreateProgramStartInfo(ProgramAction action)
    {
        var path = action.Path.Trim();
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(path) ?? ""
        };
        foreach (var argument in action.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
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

    private static Input CreateKeyboardInput(Keys key, bool keyUp)
    {
        var flags = IsExtendedKey(key) ? KeyEventFExtendedKey : 0;
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
                    ScanCode = 0,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };
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

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParamLow;
        public ushort ParamHigh;
    }
}
