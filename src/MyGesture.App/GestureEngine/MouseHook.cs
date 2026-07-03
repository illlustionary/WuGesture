using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MyGesture.App.GestureEngine;

public sealed class MouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const uint LlmhfInjected = 0x00000001;
    private const int WmMouseMove = 0x0200;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;
    private const int WmMouseWheel = 0x020A;

    private readonly LowLevelMouseProc proc;
    private IntPtr hookId;

    public MouseHook()
    {
        proc = HookCallback;
    }

    public event EventHandler<MouseHookEventArgs>? RightButtonDown;

    public event EventHandler<MouseHookEventArgs>? MouseMove;

    public event EventHandler<MouseHookEventArgs>? RightButtonUp;

    public event EventHandler<MouseHookEventArgs>? MiddleButtonDown;

    public event EventHandler<MouseHookEventArgs>? MiddleButtonUp;

    public event EventHandler<MouseWheelHookEventArgs>? MouseWheel;

    public void Start()
    {
        if (hookId != IntPtr.Zero)
        {
            return;
        }

        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        hookId = SetWindowsHookEx(WhMouseLl, proc, GetModuleHandle(currentModule?.ModuleName), 0);
        if (hookId == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install mouse hook.");
        }
    }

    public void Dispose()
    {
        if (hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        var hookStruct = Marshal.PtrToStructure<MouseHookStruct>(lParam);
        if ((hookStruct.Flags & LlmhfInjected) != 0)
        {
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        var args = new MouseHookEventArgs(new Point(hookStruct.Point.X, hookStruct.Point.Y));

        try
        {
            switch (wParam.ToInt32())
            {
                case WmRButtonDown:
                    RightButtonDown?.Invoke(this, args);
                    break;
                case WmMouseMove:
                    MouseMove?.Invoke(this, args);
                    break;
                case WmRButtonUp:
                    RightButtonUp?.Invoke(this, args);
                    break;
                case WmMButtonDown:
                    MiddleButtonDown?.Invoke(this, args);
                    break;
                case WmMButtonUp:
                    MiddleButtonUp?.Invoke(this, args);
                    break;
                case WmMouseWheel:
                    var wheelArgs = new MouseWheelHookEventArgs(
                        new Point(hookStruct.Point.X, hookStruct.Point.Y),
                        unchecked((short)((hookStruct.MouseData >> 16) & 0xffff)));
                    MouseWheel?.Invoke(this, wheelArgs);
                    args.Handled = wheelArgs.Handled;
                    break;
            }
        }
        catch
        {
            args.Handled = false;
        }

        return args.Handled ? new IntPtr(1) : CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MouseHookStruct
    {
        public readonly NativePoint Point;
        public readonly uint MouseData;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public readonly int X;
        public readonly int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
