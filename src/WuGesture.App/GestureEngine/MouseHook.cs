using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

public sealed class MouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const uint WmQuit = 0x0012;
    private const uint PmNoRemove = 0x0000;
    private const uint LlmhfInjected = 0x00000001;
    private const int WmMouseMove = 0x0200;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;
    private const int WmMouseWheel = 0x020A;

    private readonly object lifecycleLock = new();
    private readonly LowLevelMouseProc proc;
    private readonly ManualResetEventSlim startupCompleted = new(initialState: false);
    private IntPtr hookId;
    private Thread? hookThread;
    private uint hookThreadId;
    private Exception? startupException;
    private int stopRequested;
    private bool disposed;

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
        lock (lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (hookThread is not null)
            {
                return;
            }

            startupCompleted.Reset();
            startupException = null;
            Volatile.Write(ref stopRequested, 0);
            hookThread = new Thread(HookThreadMain)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = "WuGesture MouseHook"
            };
            hookThread.Start();
        }

        if (!startupCompleted.Wait(TimeSpan.FromSeconds(5)))
        {
            Dispose();
            throw new TimeoutException("Timed out while starting the mouse hook thread.");
        }

        if (startupException is { } exception)
        {
            Dispose();
            throw new InvalidOperationException("Failed to install mouse hook.", exception);
        }
    }

    public void Dispose()
    {
        Thread? thread;
        uint threadId;
        lock (lifecycleLock)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            thread = hookThread;
            threadId = hookThreadId;
        }

        Volatile.Write(ref stopRequested, 1);
        if (thread == Thread.CurrentThread)
        {
            PostQuitMessage(0);
        }
        else if (thread is not null)
        {
            if (threadId != 0)
            {
                PostThreadMessage(threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
            }

            thread.Join(TimeSpan.FromSeconds(3));
        }

        GC.SuppressFinalize(this);
    }

    private void HookThreadMain()
    {
        try
        {
            // A thread message queue does not exist until one of the queue APIs runs.
            PeekMessage(out _, IntPtr.Zero, 0, 0, PmNoRemove);
            lock (lifecycleLock)
            {
                hookThreadId = GetCurrentThreadId();
            }

            if (Volatile.Read(ref stopRequested) != 0)
            {
                startupCompleted.Set();
                return;
            }

            var installedHook = SetWindowsHookEx(WhMouseLl, proc, GetModuleHandle(null), 0);
            if (installedHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install mouse hook.");
            }

            lock (lifecycleLock)
            {
                hookId = installedHook;
            }

            startupCompleted.Set();
            var result = 0;
            while ((result = GetMessage(out var message, IntPtr.Zero, 0, 0)) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }

            if (result == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Mouse hook message loop failed.");
            }
        }
        catch (Exception exception)
        {
            if (!startupCompleted.IsSet)
            {
                startupException = exception;
            }
            else if (!disposed)
            {
                AppLogger.Error("MouseHook", "message-loop-failed", "The dedicated mouse hook message loop stopped unexpectedly.", exception);
            }

            startupCompleted.Set();
        }
        finally
        {
            IntPtr installedHook;
            lock (lifecycleLock)
            {
                installedHook = hookId;
                hookId = IntPtr.Zero;
                hookThreadId = 0;
                hookThread = null;
            }

            if (installedHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(installedHook);
            }
        }
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
    private struct NativeMessage
    {
        public IntPtr Hwnd;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public NativePoint Point;
        public uint Private;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref NativeMessage lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref NativeMessage lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(
        out NativeMessage lpMsg,
        IntPtr hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax,
        uint wRemoveMsg);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, UIntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
