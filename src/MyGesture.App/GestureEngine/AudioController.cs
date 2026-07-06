using System.Runtime.InteropServices;

namespace MyGesture.App.GestureEngine;

internal static class AudioController
{
    private static readonly Guid ClsidMmDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IidIAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static IMMDeviceEnumerator? enumerator;
    private static bool? available;

    private const int ERender = 0;
    private const int EMultimedia = 1;
    private const uint ClsctxAll = 0x17;

    public static bool IsAvailable
    {
        get
        {
            if (!TryGetEndpoint(out var endpoint))
            {
                return false;
            }

            Marshal.ReleaseComObject(endpoint);
            return true;
        }
    }

    public static float GetMasterVolume()
    {
        if (!TryGetEndpoint(out var endpoint))
        {
            return 0.5f;
        }

        try
        {
            endpoint.GetMasterVolumeLevelScalar(out var value);
            return Math.Max(0f, Math.Min(1f, value));
        }
        finally
        {
            Marshal.ReleaseComObject(endpoint);
        }
    }

    public static void SetMasterVolume(float level)
    {
        if (!TryGetEndpoint(out var endpoint))
        {
            return;
        }

        try
        {
            endpoint.SetMasterVolumeLevelScalar(Math.Max(0f, Math.Min(1f, level)), IntPtr.Zero);
        }
        finally
        {
            Marshal.ReleaseComObject(endpoint);
        }
    }

    public static bool IsMuted
    {
        get
        {
            if (!TryGetEndpoint(out var endpoint))
            {
                return false;
            }

            try
            {
                endpoint.GetMute(out var muted);
                return muted != 0;
            }
            finally
            {
                Marshal.ReleaseComObject(endpoint);
            }
        }
    }

    public static void SetMute(bool mute)
    {
        if (!TryGetEndpoint(out var endpoint))
        {
            return;
        }

        try
        {
            endpoint.SetMute(mute ? 1 : 0, IntPtr.Zero);
        }
        finally
        {
            Marshal.ReleaseComObject(endpoint);
        }
    }

    private static bool TryGetEndpoint(out IAudioEndpointVolume endpoint)
    {
        endpoint = null!;
        if (available == false)
        {
            return false;
        }

        try
        {
            if (enumerator is null)
            {
                var type = Type.GetTypeFromCLSID(ClsidMmDeviceEnumerator);
                if (type is null)
                {
                    available = false;
                    return false;
                }

                enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(type)!;
            }

            var hr = enumerator.GetDefaultAudioEndpoint(ERender, EMultimedia, out var device);
            if (hr < 0 || device is null)
            {
                available = false;
                return false;
            }

            var iid = IidIAudioEndpointVolume;
            hr = device.Activate(ref iid, ClsctxAll, IntPtr.Zero, out var volume);
            Marshal.ReleaseComObject(device);
            if (hr < 0 || volume is null)
            {
                available = false;
                return false;
            }

            endpoint = (IAudioEndpointVolume)volume;
            available = true;
            return true;
        }
        catch
        {
            available = false;
            return false;
        }
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(int dataFlow, uint stateMask, out IntPtr devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr client);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, uint clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object endpoint);

        [PreserveSig]
        int OpenPropertyStore(uint access, out IntPtr properties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

        [PreserveSig]
        int GetState(out uint state);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int GetChannelCount(out uint channelCount);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, IntPtr context);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, IntPtr context);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, IntPtr context);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, IntPtr context);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute(int mute, IntPtr context);
        [PreserveSig] int GetMute(out int mute);
        [PreserveSig] int GetVolumeStepInfo(out uint step, out uint count);
        [PreserveSig] int VolumeStepUp(IntPtr context);
        [PreserveSig] int VolumeStepDown(IntPtr context);
        [PreserveSig] int QueryHardwareSupport(out uint mask);
        [PreserveSig] int GetVolumeRange(out float min, out float max, out float increment);
        [PreserveSig] int GetVolumeRangeChannel(uint channel, out float min, out float max, out float increment);
    }
}
