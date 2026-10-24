using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

/// <summary>
/// Keeps the gesture capture decision off the low-level mouse hook. A background thread
/// evaluates the target window ahead of time and publishes an immutable snapshot; the hook
/// only compares the window handle and reads a bool. When the snapshot is stale or belongs
/// to another window, the caller falls back to an inline evaluation, so the decision stays
/// correct at the cost of one bounded lookup.
/// </summary>
internal sealed class GestureCaptureGate
{
    private const int RefreshIntervalMilliseconds = 32;
    private static readonly TimeSpan MaximumAge = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan FailureLogInterval = TimeSpan.FromSeconds(10);

    private readonly object lifecycleLock = new();
    private readonly Func<Point, IntPtr> resolveWindow;
    private readonly Func<IntPtr, bool> isBlocked;
    private Snapshot? snapshot;
    private ManualResetEventSlim? stopEvent;
    private Thread? thread;
    private long lastFailureLogTimestamp;

    public GestureCaptureGate(Func<Point, IntPtr> resolveWindow, Func<IntPtr, bool> isBlocked)
    {
        this.resolveWindow = resolveWindow;
        this.isBlocked = isBlocked;
    }

    public void Start()
    {
        lock (lifecycleLock)
        {
            if (thread is not null)
            {
                return;
            }

            var stop = new ManualResetEventSlim(initialState: false);
            stopEvent = stop;
            var worker = new Thread(() => Loop(stop))
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "WuGesture Capture Gate"
            };
            thread = worker;
            worker.Start();
        }
    }

    public void Stop()
    {
        Thread? worker;
        ManualResetEventSlim? stop;
        lock (lifecycleLock)
        {
            worker = thread;
            stop = stopEvent;
            thread = null;
            stopEvent = null;
            snapshot = null;
        }

        if (stop is null)
        {
            return;
        }

        stop.Set();
        if (worker is not null && worker != Thread.CurrentThread)
        {
            worker.Join(TimeSpan.FromSeconds(1));
        }

        stop.Dispose();
    }

    public bool TryGetDecision(IntPtr window, out bool blocked)
    {
        blocked = false;
        var current = Volatile.Read(ref snapshot);
        if (current is null || window == IntPtr.Zero || current.Window != window)
        {
            return false;
        }

        if (Stopwatch.GetElapsedTime(current.CapturedAt) >= MaximumAge)
        {
            return false;
        }

        blocked = current.Blocked;
        return true;
    }

    private void Loop(ManualResetEventSlim stop)
    {
        while (true)
        {
            Refresh();
            bool stopRequested;
            try
            {
                stopRequested = stop.Wait(RefreshIntervalMilliseconds);
            }
            catch (ObjectDisposedException)
            {
                // Stop() only disposes the event after setting it and waiting for this thread.
                return;
            }

            if (stopRequested)
            {
                return;
            }
        }
    }

    private void Refresh()
    {
        try
        {
            GetCursorPos(out var cursor);
            var window = resolveWindow(new Point(cursor.X, cursor.Y));
            if (window == IntPtr.Zero)
            {
                Volatile.Write(ref snapshot, null);
                return;
            }

            Volatile.Write(ref snapshot, new Snapshot(window, isBlocked(window), Stopwatch.GetTimestamp()));
        }
        catch (Exception exception)
        {
            LogFailure(exception);
        }
    }

    private void LogFailure(Exception exception)
    {
        var now = Stopwatch.GetTimestamp();
        var previous = Volatile.Read(ref lastFailureLogTimestamp);
        if (previous != 0 && Stopwatch.GetElapsedTime(previous, now) < FailureLogInterval)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref lastFailureLogTimestamp, now, previous) != previous)
        {
            return;
        }

        AppLogger.Warning(
            "GestureCaptureGate",
            "refresh-failed",
            "Refreshing the gesture capture snapshot failed.",
            exception);
    }

    private sealed record Snapshot(IntPtr Window, bool Blocked, long CapturedAt);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);
}
