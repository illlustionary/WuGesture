using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public sealed class KeyboardShortcutRecorder : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const uint LlkInjected = 0x00000010;

    private readonly LowLevelKeyboardProc proc;
    private readonly HashSet<Keys> pressedKeys = [];
    private readonly List<Keys> recordedKeys = [];
    private SynchronizationContext? synchronizationContext;
    private IntPtr hookId;
    private string requestId = "";
    private bool isRecording;
    private bool disposed;

    public KeyboardShortcutRecorder()
    {
        proc = HookCallback;
    }

    public event EventHandler<HotkeyRecordedEventArgs>? HotkeyRecorded;

    public void Start(string nextRequestId)
    {
        if (disposed)
        {
            return;
        }

        synchronizationContext ??= SynchronizationContext.Current;
        requestId = nextRequestId;
        pressedKeys.Clear();
        recordedKeys.Clear();
        EnsureHook();
        isRecording = true;
    }

    public void Stop()
    {
        isRecording = false;
        requestId = "";
        pressedKeys.Clear();
        recordedKeys.Clear();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Stop();
        if (hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }

    private void EnsureHook()
    {
        if (hookId != IntPtr.Zero)
        {
            return;
        }

        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        hookId = SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(currentModule?.ModuleName), 0);
        if (hookId == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install keyboard hook.");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || !isRecording)
        {
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        var hookStruct = Marshal.PtrToStructure<KeyboardHookStruct>(lParam);
        if ((hookStruct.Flags & LlkInjected) != 0)
        {
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        var message = wParam.ToInt32();
        var key = NormalizeKey((Keys)hookStruct.VirtualKeyCode);
        if (key == Keys.None)
        {
            return new IntPtr(1);
        }

        if (message is WmKeyDown or WmSysKeyDown)
        {
            pressedKeys.Add(key);
            if (!recordedKeys.Contains(key))
            {
                recordedKeys.Add(key);
            }
        }
        else if (message is WmKeyUp or WmSysKeyUp)
        {
            pressedKeys.Remove(key);
            if (pressedKeys.Count == 0 && recordedKeys.Count > 0)
            {
                CompleteRecording();
            }
        }

        return new IntPtr(1);
    }

    private void CompleteRecording()
    {
        var completedRequestId = requestId;
        var keys = OrderKeys(recordedKeys).Select(ToConfigKeyName).ToArray();

        Stop();
        Post(() =>
        {
            if (!disposed)
            {
                HotkeyRecorded?.Invoke(this, new HotkeyRecordedEventArgs(completedRequestId, keys));
            }
        });
    }

    private void Post(Action action)
    {
        var context = synchronizationContext;
        if (context is null)
        {
            action();
            return;
        }

        context.Post(_ => action(), null);
    }

    private static Keys NormalizeKey(Keys key)
    {
        return key switch
        {
            Keys.LControlKey or Keys.RControlKey or Keys.ControlKey => Keys.ControlKey,
            Keys.LShiftKey or Keys.RShiftKey or Keys.ShiftKey => Keys.ShiftKey,
            Keys.LMenu or Keys.RMenu or Keys.Menu => Keys.Menu,
            Keys.LWin or Keys.RWin => Keys.LWin,
            _ => key
        };
    }

    private static IEnumerable<Keys> OrderKeys(IEnumerable<Keys> keys)
    {
        var keyList = keys.ToList();
        foreach (var modifier in new[] { Keys.ControlKey, Keys.Menu, Keys.ShiftKey, Keys.LWin })
        {
            if (keyList.Remove(modifier))
            {
                yield return modifier;
            }
        }

        foreach (var key in keyList)
        {
            yield return key;
        }
    }

    private static string ToConfigKeyName(Keys key)
    {
        return key switch
        {
            Keys.Menu => "Alt",
            Keys.ControlKey => "Control",
            Keys.ShiftKey => "Shift",
            Keys.LWin => "Win",
            _ => key.ToString()
        };
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct KeyboardHookStruct
    {
        public readonly uint VirtualKeyCode;
        public readonly uint ScanCode;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

public sealed class HotkeyRecordedEventArgs : EventArgs
{
    public HotkeyRecordedEventArgs(string requestId, IReadOnlyList<string> keys)
    {
        RequestId = requestId;
        Keys = keys;
    }

    public string RequestId { get; }

    public IReadOnlyList<string> Keys { get; }
}
