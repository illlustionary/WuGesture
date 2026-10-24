using System.Runtime.InteropServices;

namespace WuGesture.App;

/// <summary>
/// Owns the notification-area icon and its menu, so the main form only reacts to the three user actions
/// and never touches tray icon resources directly.
/// </summary>
internal sealed class TrayMenuController : IDisposable
{
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly ToolStripMenuItem pauseItem;
    private readonly Icon normalTrayIcon;
    private readonly Icon pausedTrayIcon;

    public TrayMenuController(Action openConfiguration, Action<bool> setUserPaused, Action exit)
    {
        pauseItem = new ToolStripMenuItem($"暂停 {AppIdentity.DisplayName}")
        {
            CheckOnClick = true
        };
        pauseItem.CheckedChanged += (_, _) => setUserPaused(pauseItem.Checked);

        trayMenu.Items.Add(new ToolStripMenuItem("打开配置", null, (_, _) => openConfiguration()));
        trayMenu.Items.Add(pauseItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(new ToolStripMenuItem("退出", null, (_, _) => exit()));

        normalTrayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        pausedTrayIcon = CreateGrayscaleIcon(normalTrayIcon);
        trayIcon.Text = AppIdentity.DisplayName;
        trayIcon.Icon = normalTrayIcon;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => openConfiguration();
    }

    public void SetPaused(bool paused)
    {
        if (pauseItem.Checked != paused)
        {
            pauseItem.Checked = paused;
        }

        pauseItem.Text = paused ? $"恢复 {AppIdentity.DisplayName}" : $"暂停 {AppIdentity.DisplayName}";
        trayIcon.Text = paused ? $"{AppIdentity.DisplayName}（已暂停）" : AppIdentity.DisplayName;
        trayIcon.Icon = paused ? pausedTrayIcon : normalTrayIcon;
    }

    public void Dispose()
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
        normalTrayIcon.Dispose();
        pausedTrayIcon.Dispose();
    }

    private static Icon CreateGrayscaleIcon(Icon source)
    {
        using var bitmap = source.ToBitmap();
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                var gray = (int)Math.Round(color.R * 0.299 + color.G * 0.587 + color.B * 0.114);
                bitmap.SetPixel(x, y, Color.FromArgb(color.A, gray, gray, gray));
            }
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
