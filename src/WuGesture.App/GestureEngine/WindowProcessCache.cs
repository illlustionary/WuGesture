using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

internal readonly record struct WindowProcessInfo(int ProcessId, string ProcessName, string ExecutablePath);

/// <summary>
/// Short-lived window to process identity cache. Foreground window lookups happen on the
/// mouse hook, the gesture parser and the 16 ms edge-action poll, so a repeated query for
/// the same window must stay a dictionary hit. Entries carry the process id so a reused
/// window handle cannot serve stale identity.
/// </summary>
internal static class WindowProcessCache
{
    private const int Capacity = 64;
    private static readonly TimeSpan MaximumAge = TimeSpan.FromSeconds(1);
    private static readonly object Gate = new();
    private static readonly Dictionary<IntPtr, Entry> Entries = new();

    public static bool TryGet(IntPtr window, bool includeExecutablePath, out WindowProcessInfo info)
    {
        info = default;
        if (window == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(window, out var processId);
        if (processId <= 0)
        {
            return false;
        }

        var capturedAt = Stopwatch.GetTimestamp();
        if (TryGetCached(window, processId, includeExecutablePath, capturedAt, out info))
        {
            return true;
        }

        if (!ProcessImageQuery.TryGetIdentity(processId, out var processName, out var executablePath))
        {
            return false;
        }

        info = new WindowProcessInfo(processId, processName, executablePath);
        Remember(window, info, capturedAt);
        if (!includeExecutablePath)
        {
            info = info with { ExecutablePath = "" };
        }

        return true;
    }

    private static bool TryGetCached(
        IntPtr window,
        int processId,
        bool includeExecutablePath,
        long now,
        out WindowProcessInfo info)
    {
        info = default;
        lock (Gate)
        {
            if (!Entries.TryGetValue(window, out var entry) ||
                entry.ProcessId != processId ||
                Stopwatch.GetElapsedTime(entry.CapturedAt, now) >= MaximumAge)
            {
                return false;
            }

            info = new WindowProcessInfo(
                entry.ProcessId,
                entry.ProcessName,
                includeExecutablePath ? entry.ExecutablePath : "");
            return true;
        }
    }

    private static void Remember(IntPtr window, WindowProcessInfo info, long capturedAt)
    {
        lock (Gate)
        {
            Entries[window] = new Entry(info.ProcessId, info.ProcessName, info.ExecutablePath, capturedAt);
            if (Entries.Count <= Capacity)
            {
                return;
            }

            RemoveExpired(capturedAt);
            while (Entries.Count > Capacity)
            {
                var oldestWindow = IntPtr.Zero;
                var oldestCapturedAt = long.MaxValue;
                foreach (var pair in Entries)
                {
                    if (pair.Value.CapturedAt >= oldestCapturedAt)
                    {
                        continue;
                    }

                    oldestCapturedAt = pair.Value.CapturedAt;
                    oldestWindow = pair.Key;
                }

                if (!Entries.Remove(oldestWindow))
                {
                    break;
                }
            }
        }
    }

    private static void RemoveExpired(long now)
    {
        List<IntPtr>? expired = null;
        foreach (var pair in Entries)
        {
            if (Stopwatch.GetElapsedTime(pair.Value.CapturedAt, now) < MaximumAge)
            {
                continue;
            }

            expired ??= [];
            expired.Add(pair.Key);
        }

        if (expired is null)
        {
            return;
        }

        foreach (var window in expired)
        {
            Entries.Remove(window);
        }
    }

    private readonly record struct Entry(int ProcessId, string ProcessName, string ExecutablePath, long CapturedAt);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);
}
