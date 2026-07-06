using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;

namespace MyGesture.App.GestureEngine;

internal static class BrightnessController
{
    private const uint MonitorDefaultToPrimary = 1;
    private static bool? ddcAvailable;
    private static IntPtr cachedPhysicalMonitor;
    private static IntPtr cachedMonitorForCleanup;
    private static bool? wmiAvailable;
    private static bool? gammaAvailable;
    private static int lastGammaBrightness = 100;
    private static GammaRamp? originalGammaRamp;
    private static uint ddcMinBrightness;
    private static uint ddcMaxBrightness;

    public static int GetBrightness()
    {
        if (TryInitDdc())
        {
            try
            {
                if (GetMonitorBrightness(cachedPhysicalMonitor, out var min, out var current, out var max))
                {
                    ddcMinBrightness = min;
                    ddcMaxBrightness = max;
                    return max > min ? (int)((current - min) * 100 / (max - min)) : (int)current;
                }
            }
            catch
            {
            }
        }

        if (TryInitWmi())
        {
            var wmi = GetWmiBrightness();
            if (wmi >= 0)
            {
                return wmi;
            }
        }

        return lastGammaBrightness;
    }

    public static void SetBrightness(int level)
    {
        level = Math.Max(0, Math.Min(100, level));

        if (TryInitDdc())
        {
            try
            {
                if (ddcMaxBrightness > ddcMinBrightness)
                {
                    var target = (uint)(ddcMinBrightness + (ddcMaxBrightness - ddcMinBrightness) * level / 100d);
                    if (SetMonitorBrightness(cachedPhysicalMonitor, target))
                    {
                        RestoreOriginalGamma();
                        return;
                    }
                }
            }
            catch
            {
            }
        }

        if (TryInitWmi())
        {
            try
            {
                SetWmiBrightness(level);
                RestoreOriginalGamma();
                return;
            }
            catch
            {
            }
        }

        if (TryInitGamma())
        {
            ApplyGamma(level);
            lastGammaBrightness = level;
        }
    }

    public static void InvalidateCache()
    {
        RestoreOriginalGamma();

        if (cachedPhysicalMonitor != IntPtr.Zero)
        {
            try
            {
                var monitors = new PhysicalMonitor[1];
                monitors[0].Handle = cachedPhysicalMonitor;
                monitors[0].Description = "";
                DestroyPhysicalMonitors(1, monitors);
            }
            catch
            {
            }
        }

        cachedPhysicalMonitor = IntPtr.Zero;
        cachedMonitorForCleanup = IntPtr.Zero;
        ddcAvailable = null;
        wmiAvailable = null;
        gammaAvailable = null;
        ddcMinBrightness = 0;
        ddcMaxBrightness = 0;
        lastGammaBrightness = 100;
    }

    private static bool TryInitDdc()
    {
        if (ddcAvailable.HasValue)
        {
            return ddcAvailable.Value;
        }

        try
        {
            var monitor = MonitorFromPoint(new Point(0, 0), MonitorDefaultToPrimary);
            if (monitor == IntPtr.Zero ||
                !GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out var count) ||
                count == 0)
            {
                ddcAvailable = false;
                return false;
            }

            var monitors = new PhysicalMonitor[count];
            if (!GetPhysicalMonitorsFromHMONITOR(monitor, count, monitors))
            {
                ddcAvailable = false;
                return false;
            }

            var ok = monitors.Length > 0 &&
                monitors[0].Handle != IntPtr.Zero &&
                GetMonitorBrightness(monitors[0].Handle, out _, out _, out _);

            if (ok)
            {
                GetMonitorBrightness(monitors[0].Handle, out ddcMinBrightness, out _, out ddcMaxBrightness);
                cachedPhysicalMonitor = monitors[0].Handle;
                cachedMonitorForCleanup = monitor;
            }

            for (var i = ok ? 1 : 0; i < monitors.Length; i++)
            {
                if (monitors[i].Handle != IntPtr.Zero)
                {
                    DestroyPhysicalMonitors(1, [monitors[i]]);
                }
            }

            ddcAvailable = ok;
            return ok;
        }
        catch
        {
            ddcAvailable = false;
            return false;
        }
    }

    private static bool TryInitWmi()
    {
        if (wmiAvailable.HasValue)
        {
            return wmiAvailable.Value;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightness");
            foreach (ManagementObject _ in searcher.Get())
            {
                wmiAvailable = true;
                return true;
            }
        }
        catch
        {
        }

        wmiAvailable = false;
        return false;
    }

    private static int GetWmiBrightness()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightness");
            foreach (ManagementObject item in searcher.Get())
            {
                return Convert.ToInt32(item["CurrentBrightness"]);
            }
        }
        catch
        {
        }

        return -1;
    }

    private static void SetWmiBrightness(int brightness)
    {
        using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
        foreach (ManagementObject item in searcher.Get())
        {
            item.InvokeMethod("WmiSetBrightness", [1, (byte)brightness]);
            return;
        }
    }

    private static bool TryInitGamma()
    {
        if (gammaAvailable.HasValue)
        {
            return gammaAvailable.Value;
        }

        try
        {
            var dc = GetDC(IntPtr.Zero);
            if (dc == IntPtr.Zero)
            {
                gammaAvailable = false;
                return false;
            }

            var ok = GetDeviceGammaRamp(dc, out var ramp);
            ReleaseDC(IntPtr.Zero, dc);
            if (ok)
            {
                originalGammaRamp = ramp;
                gammaAvailable = true;
                return true;
            }
        }
        catch
        {
        }

        gammaAvailable = false;
        return false;
    }

    private static void ApplyGamma(int brightness)
    {
        if (!originalGammaRamp.HasValue)
        {
            return;
        }

        var scale = 0.1f + brightness / 100f * 0.9f;
        var source = originalGammaRamp.Value;
        var ramp = new GammaRamp
        {
            Red = new ushort[256],
            Green = new ushort[256],
            Blue = new ushort[256]
        };

        for (var i = 0; i < 256; i++)
        {
            ramp.Red[i] = (ushort)Math.Min(ushort.MaxValue, source.Red[i] * scale);
            ramp.Green[i] = (ushort)Math.Min(ushort.MaxValue, source.Green[i] * scale);
            ramp.Blue[i] = (ushort)Math.Min(ushort.MaxValue, source.Blue[i] * scale);
        }

        var dc = GetDC(IntPtr.Zero);
        if (dc == IntPtr.Zero)
        {
            return;
        }

        SetDeviceGammaRamp(dc, ref ramp);
        ReleaseDC(IntPtr.Zero, dc);
    }

    private static void RestoreOriginalGamma()
    {
        if (!originalGammaRamp.HasValue)
        {
            return;
        }

        var ramp = originalGammaRamp.Value;
        var dc = GetDC(IntPtr.Zero);
        if (dc != IntPtr.Zero)
        {
            SetDeviceGammaRamp(dc, ref ramp);
            ReleaseDC(IntPtr.Zero, dc);
        }

        originalGammaRamp = null;
        lastGammaBrightness = 100;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PhysicalMonitor
    {
        public IntPtr Handle;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct GammaRamp
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Red;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Green;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Blue;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point point, uint flags);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr monitor, out uint count);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr monitor, uint arraySize, [Out] PhysicalMonitor[] monitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetMonitorBrightness(IntPtr physicalMonitor, out uint min, out uint current, out uint max);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool SetMonitorBrightness(IntPtr physicalMonitor, uint brightness);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(uint arraySize, [In] PhysicalMonitor[] monitors);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool SetDeviceGammaRamp(IntPtr hdc, ref GammaRamp ramp);

    [DllImport("gdi32.dll")]
    private static extern bool GetDeviceGammaRamp(IntPtr hdc, out GammaRamp ramp);
}
