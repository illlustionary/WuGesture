using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WuGesture.App;

internal static class ApplicationIconDataUrl
{
    private const int MaxCachedIcons = 128;
    private const uint ShgfiIcon = 0x00000100;
    private const uint ShgfiUseFileAttributes = 0x00000010;
    private const uint FileAttributeNormal = 0x00000080;
    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, CachedIcon> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts the icon of a configured path as a PNG data URL. Executables and shortcuts use their
    /// embedded icon; documents fall back to the icon of their registered handler so rules that open
    /// a file show the same glyph Explorer does. Every <c>rules</c> payload asks for the icons of all
    /// configured programs and exclusions, so results are cached until the file itself changes.
    /// </summary>
    public static string FromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "";
        }

        FileStamp stamp;
        try
        {
            var file = new FileInfo(path);
            stamp = new FileStamp(file.Length, file.LastWriteTimeUtc.Ticks);
        }
        catch
        {
            return "";
        }

        lock (CacheLock)
        {
            if (Cache.TryGetValue(path, out var cached) && cached.Stamp == stamp)
            {
                return cached.DataUrl;
            }
        }

        var dataUrl = Extract(path);
        lock (CacheLock)
        {
            if (Cache.Count >= MaxCachedIcons)
            {
                Cache.Clear();
            }

            Cache[path] = new CachedIcon(stamp, dataUrl);
        }

        return dataUrl;
    }

    private static string Extract(string path)
    {
        using var icon = ExtractIcon(path);
        if (icon is null)
        {
            return "";
        }

        try
        {
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
        }
        catch
        {
            return "";
        }
    }

    private static Icon? ExtractIcon(string path)
    {
        try
        {
            if (Icon.ExtractAssociatedIcon(path) is { } embedded)
            {
                return embedded;
            }
        }
        catch
        {
            // Documents usually have no embedded icon; fall through to the shell icon below.
        }

        return ShellIcon(path);
    }

    private static Icon? ShellIcon(string path)
    {
        var info = new ShFileInfo();
        var result = SHGetFileInfo(
            path,
            FileAttributeNormal,
            ref info,
            (uint)Marshal.SizeOf<ShFileInfo>(),
            ShgfiIcon | ShgfiUseFileAttributes);
        if (result == IntPtr.Zero || info.IconHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            // Clone so the icon survives DestroyIcon and can be disposed by the caller.
            return (Icon)Icon.FromHandle(info.IconHandle).Clone();
        }
        finally
        {
            DestroyIcon(info.IconHandle);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr IconHandle;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string path,
        uint fileAttributes,
        ref ShFileInfo info,
        uint infoSize,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);

    private readonly record struct FileStamp(long Length, long LastWriteTimeUtcTicks);

    private sealed record CachedIcon(FileStamp Stamp, string DataUrl);
}
