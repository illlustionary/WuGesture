using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MyGesture.App.GestureEngine;

public sealed class ActionExecutor
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventFExtendedKey = 0x0001;
    private const uint KeyEventFKeyUp = 0x0002;

    public void Execute(GestureRule rule)
    {
        ExecuteHotkey(rule.Action);
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
            Keys.NumLock or
            Keys.PrintScreen or
            Keys.RMenu or
            Keys.RControlKey;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

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
