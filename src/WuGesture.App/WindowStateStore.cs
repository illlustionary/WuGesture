using System.Text.Json;

namespace WuGesture.App;

internal sealed class WindowStateStore
{
    private const int DefaultWindowWidth = 1080;
    private const int DefaultWindowHeight = 720;
    private const int MinimumWindowWidth = 640;
    private const int MinimumWindowHeight = 480;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppIdentity.AppDataFolderName,
        ConfigStorageContract.WindowStateFileName);

    public bool TryLoad(out Rectangle bounds, out bool maximized)
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

            bounds = NormalizeBounds(state.Bounds);
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

    public Rectangle GetDefaultBounds()
    {
        var workingArea = GetWorkingArea();
        var width = Math.Min(DefaultWindowWidth, workingArea.Width);
        var height = Math.Min(DefaultWindowHeight, workingArea.Height);
        var left = workingArea.Left + Math.Max(0, (workingArea.Width - width) / 2);
        var top = workingArea.Top + Math.Max(0, (workingArea.Height - height) / 2);
        return new Rectangle(left, top, width, height);
    }

    private static Rectangle NormalizeBounds(Rectangle bounds)
    {
        var workingArea = GetWorkingArea();
        var width = Math.Min(Math.Max(MinimumWindowWidth, bounds.Width), workingArea.Width);
        var height = Math.Min(Math.Max(MinimumWindowHeight, bounds.Height), workingArea.Height);
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

    private static Rectangle GetWorkingArea()
    {
        return Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
    }

    private static bool HasUsableWindowSize(Rectangle bounds)
    {
        return bounds.Width >= MinimumWindowWidth && bounds.Height >= MinimumWindowHeight;
    }

    private sealed record WindowStateData(int X, int Y, int Width, int Height, bool Maximized)
    {
        public Rectangle Bounds => new(X, Y, Width, Height);
    }
}
