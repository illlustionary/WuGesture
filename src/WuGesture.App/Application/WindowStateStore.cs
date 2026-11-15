using System.Text.Json;

namespace WuGesture.App;

internal sealed class WindowStateStore
{
    // Logical sizes are DIPs; the store converts them with the current monitor DPI.
    internal const int LogicalDefaultWindowWidth = 1280;
    internal const int LogicalDefaultWindowHeight = 720;
    internal const int LogicalMinimumWindowWidth = 1280;
    internal const int LogicalMinimumWindowHeight = 720;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppIdentity.AppDataFolderName,
        ConfigStorageContract.WindowStateFileName);

    public static int Scale(int logicalPixels, int dpi)
    {
        var effectiveDpi = dpi > 0 ? dpi : 96;
        return (int)Math.Round(logicalPixels * effectiveDpi / 96.0, MidpointRounding.AwayFromZero);
    }

    // The minimum window size in physical pixels, never larger than the working area.
    public static Size GetMinimumSize(int dpi, Rectangle workingArea)
    {
        var maxWidth = Math.Max(1, workingArea.Width);
        var maxHeight = Math.Max(1, workingArea.Height);
        return new Size(
            Math.Min(Scale(LogicalMinimumWindowWidth, dpi), maxWidth),
            Math.Min(Scale(LogicalMinimumWindowHeight, dpi), maxHeight));
    }

    public static Rectangle GetPrimaryWorkingArea()
    {
        return Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
    }

    public bool TryLoad(int dpi, out Rectangle bounds, out bool maximized)
    {
        bounds = Rectangle.Empty;
        maximized = false;

        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var state = JsonSerializer.Deserialize<WindowStateData>(File.ReadAllText(filePath), JsonOptions);
            if (state is null || !HasUsableWindowSize(state.Bounds))
            {
                return false;
            }

            // Saved bounds are physical pixels; use the monitor they were saved on when it still exists.
            bounds = NormalizeBounds(state.Bounds, dpi, GetWorkingArea(state.Bounds));
            maximized = state.Maximized;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Save(FormWindowState windowState, Rectangle bounds, Rectangle restoreBounds)
    {
        try
        {
            var savedBounds = windowState == FormWindowState.Normal ? bounds : restoreBounds;
            if (!HasUsableWindowSize(savedBounds))
            {
                return;
            }

            var state = new WindowStateData(
                savedBounds.X,
                savedBounds.Y,
                savedBounds.Width,
                savedBounds.Height,
                windowState == FormWindowState.Maximized);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, JsonSerializer.Serialize(state, JsonOptions));
        }
        catch
        {
        }
    }

    public static Rectangle GetDefaultBounds(int dpi, Rectangle workingArea)
    {
        var maxWidth = Math.Max(1, workingArea.Width);
        var maxHeight = Math.Max(1, workingArea.Height);
        var width = Math.Min(Scale(LogicalDefaultWindowWidth, dpi), maxWidth);
        var height = Math.Min(Scale(LogicalDefaultWindowHeight, dpi), maxHeight);
        var left = workingArea.Left + Math.Max(0, (workingArea.Width - width) / 2);
        var top = workingArea.Top + Math.Max(0, (workingArea.Height - height) / 2);
        return new Rectangle(left, top, width, height);
    }

    public static Rectangle NormalizeBounds(Rectangle bounds, int dpi, Rectangle workingArea)
    {
        var minimum = GetMinimumSize(dpi, workingArea);
        var width = Math.Min(Math.Max(bounds.Width, minimum.Width), Math.Max(1, workingArea.Width));
        var height = Math.Min(Math.Max(bounds.Height, minimum.Height), Math.Max(1, workingArea.Height));
        var left = bounds.Left;
        var top = bounds.Top;

        if (left < workingArea.Left || left + width > workingArea.Right)
        {
            left = workingArea.Left + Math.Max(0, (workingArea.Width - width) / 2);
        }

        if (top < workingArea.Top || top + height > workingArea.Bottom)
        {
            top = workingArea.Top + Math.Max(0, (workingArea.Height - height) / 2);
        }

        return new Rectangle(left, top, width, height);
    }

    private static Rectangle GetWorkingArea(Rectangle anchor)
    {
        return Screen.FromRectangle(anchor)?.WorkingArea
            ?? Screen.PrimaryScreen?.WorkingArea
            ?? new Rectangle(0, 0, 1280, 720);
    }

    private static bool HasUsableWindowSize(Rectangle bounds)
    {
        return bounds.Width > 0 && bounds.Height > 0;
    }

    private sealed record WindowStateData(int X, int Y, int Width, int Height, bool Maximized)
    {
        public Rectangle Bounds => new(X, Y, Width, Height);
    }
}
