using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WuGesture.App.GestureEngine;

/// <summary>
/// Reads the process identity behind a window with the cheapest available Win32 call.
/// Module enumeration is deliberately avoided: protected processes (security products)
/// can make it take hundreds of milliseconds, and these lookups run on the mouse hook,
/// the gesture parser and the UI polling thread.
/// </summary>
internal static class ProcessImageQuery
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const int SlowQueryThresholdMilliseconds = 50;

    public static bool TryGetIdentity(int processId, out string processName, out string executablePath)
    {
        processName = "";
        executablePath = "";

        if (processId <= 0)
        {
            return false;
        }

        if (TryGetExecutablePath(processId, out executablePath))
        {
            processName = Path.GetFileNameWithoutExtension(executablePath);
            return processName.Length > 0;
        }

        processName = GetProcessNameFromProcessApi(processId);
        return processName.Length > 0;
    }

    public static string GetProcessName(int processId)
    {
        return TryGetIdentity(processId, out var processName, out _) ? processName : "";
    }

    private static bool TryGetExecutablePath(int processId, out string executablePath)
    {
        executablePath = "";
        var startedAt = Stopwatch.GetTimestamp();
        var processHandle = OpenProcess(ProcessQueryLimitedInformation, bInheritHandle: false, processId);
        if (processHandle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var buffer = new StringBuilder(1024);
            var length = buffer.Capacity;
            if (!QueryFullProcessImageName(processHandle, 0, buffer, ref length))
            {
                return false;
            }

            executablePath = buffer.ToString(0, length);
            return executablePath.Length > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            CloseHandle(processHandle);
            LogIfSlow(startedAt, processId);
        }
    }

    private static string GetProcessNameFromProcessApi(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return "";
        }
    }

    private static void LogIfSlow(long startedAt, int processId)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        if (elapsedMs < SlowQueryThresholdMilliseconds)
        {
            return;
        }

        AppLogger.Warning(
            "ProcessImageQuery",
            "slow-image-query",
            $"Querying the image path for process {processId} took {elapsedMs:F1} ms.");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        IntPtr process,
        uint flags,
        StringBuilder executableName,
        ref int size);
}
