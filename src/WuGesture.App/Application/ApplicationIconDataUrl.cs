using System.Drawing.Imaging;

namespace WuGesture.App;

internal static class ApplicationIconDataUrl
{
    private const int MaxCachedIcons = 128;
    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, CachedIcon> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts the executable icon as a PNG data URL. Every <c>rules</c> payload asks for the icons of all
    /// configured programs and exclusions, so results are cached until the executable file itself changes.
    /// </summary>
    public static string FromExecutable(string path)
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
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null)
            {
                return "";
            }

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

    private readonly record struct FileStamp(long Length, long LastWriteTimeUtcTicks);

    private sealed record CachedIcon(FileStamp Stamp, string DataUrl);
}
