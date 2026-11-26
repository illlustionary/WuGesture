using System.Drawing.Imaging;
using System.Text;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

/// <summary>
/// Owns the notification-area icon and its menu, so the main form only reacts to the three user actions
/// and never touches tray icon resources directly. The menu uses a custom renderer and a rounded region so
/// it matches the configuration window instead of looking like the default WinForms drop-down.
/// </summary>
internal sealed class TrayMenuController : IDisposable
{
    private const int MenuCornerRadius = 9;
    private const int MenuInset = 5;
    private const int ItemLeftPadding = 24;
    private const int ItemRightPadding = 18;
    private const int ItemVerticalPadding = 7;

    /// <summary>
    /// Matches the paused brand mark in the configuration sidebar (`filter: grayscale(1); opacity: .54`):
    /// the icon is desaturated and made translucent instead of being darkened into a gray disc.
    /// </summary>
    private const float PausedIconOpacity = 0.54f;

    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly ToolStripMenuItem openItem;
    private readonly ToolStripMenuItem pauseItem;
    private readonly ToolStripMenuItem exitItem;
    private readonly Font menuFont;
    private readonly Icon normalTrayIcon;
    private readonly Icon pausedTrayIcon;
    private readonly float dpiScale;
    private TrayMenuPalette? appliedPalette;

    public TrayMenuController(
        Action openConfiguration,
        Action<bool> setUserPaused,
        Action exit,
        AppearanceUiSettings appearance)
    {
        dpiScale = GetPrimaryDpiScale();
        menuFont = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

        openItem = new ToolStripMenuItem("打开配置", null, (_, _) => openConfiguration());
        pauseItem = new ToolStripMenuItem($"暂停 {AppIdentity.DisplayName}") { CheckOnClick = true };
        pauseItem.CheckedChanged += (_, _) => setUserPaused(pauseItem.Checked);
        exitItem = new ToolStripMenuItem("退出", null, (_, _) => exit());
        var separator = new ToolStripSeparator();

        trayMenu.Font = menuFont;
        trayMenu.ShowImageMargin = false;
        trayMenu.ShowCheckMargin = false;
        trayMenu.Padding = new Padding(Scale(MenuInset));
        trayMenu.Items.AddRange(new ToolStripItem[] { openItem, pauseItem, separator, exitItem });
        ApplyItemMetrics(separator);
        trayMenu.Opened += (_, _) => UpdateMenuRegion();
        trayMenu.SizeChanged += (_, _) => UpdateMenuRegion();
        ApplyAppearance(appearance);

        normalTrayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        pausedTrayIcon = CreatePausedIcon(normalTrayIcon);
        trayIcon.Text = AppIdentity.DisplayName;
        trayIcon.Icon = normalTrayIcon;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => openConfiguration();
    }

    /// <summary>
    /// The tray menu uses a dedicated check indicator instead of the checkable item's built-in check box, so
    /// the paused state is driven explicitly to keep the label and dot in sync.
    /// </summary>
    public void SetPaused(bool paused)
    {
        pauseItem.Checked = paused;
        pauseItem.Text = paused ? $"恢复 {AppIdentity.DisplayName}" : $"暂停 {AppIdentity.DisplayName}";
        trayIcon.Text = paused ? $"{AppIdentity.DisplayName}（已暂停）" : AppIdentity.DisplayName;
        trayIcon.Icon = paused ? pausedTrayIcon : normalTrayIcon;
        UpdateMenuRegion();
    }

    public void ApplyAppearance(AppearanceUiSettings appearance)
    {
        var dark = appearance.Theme == GestureConfigContract.AppearanceThemes.Dark ||
            (appearance.Theme == GestureConfigContract.AppearanceThemes.System && SystemTheme.PrefersDarkMode());
        var palette = TrayMenuPalette.FromAppearance(appearance, dark);
        if (appliedPalette is { } applied && applied == palette)
        {
            return;
        }

        appliedPalette = palette;
        trayMenu.BackColor = palette.Background;
        trayMenu.ForeColor = palette.Text;
        trayMenu.Renderer = new TrayMenuRenderer(palette, ItemLeftPadding, ItemRightPadding);
        foreach (ToolStripItem item in trayMenu.Items)
        {
            item.BackColor = palette.Background;
            item.ForeColor = palette.Text;
        }

        UpdateMenuRegion();
        trayMenu.Invalidate();
    }

    public void Dispose()
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
        menuFont.Dispose();
        normalTrayIcon.Dispose();
        pausedTrayIcon.Dispose();
    }

    private void ApplyItemMetrics(ToolStripSeparator separator)
    {
        var padding = new Padding(
            Scale(ItemLeftPadding),
            Scale(ItemVerticalPadding),
            Scale(ItemRightPadding),
            Scale(ItemVerticalPadding));
        foreach (var item in new[] { openItem, pauseItem, exitItem })
        {
            item.Padding = padding;
            item.Margin = new Padding(0, Scale(1), 0, 0);
        }

        exitItem.Margin = new Padding(0, Scale(1), 0, Scale(1));
        separator.Margin = new Padding(0, Scale(3), 0, Scale(3));
    }

    private void UpdateMenuRegion()
    {
        if (trayMenu.Width <= 0 || trayMenu.Height <= 0)
        {
            return;
        }

        using var path = TrayMenuRenderer.CreateRoundedPath(
            new Rectangle(Point.Empty, trayMenu.Size),
            Scale(MenuCornerRadius));
        var region = new Region(path);
        var previous = trayMenu.Region;
        trayMenu.Region = region;
        previous?.Dispose();
    }

    private int Scale(int value)
    {
        return (int)Math.Round(value * dpiScale);
    }

    private static float GetPrimaryDpiScale()
    {
        using var graphics = Graphics.FromHwnd(IntPtr.Zero);
        return Math.Max(1f, graphics.DpiX / 96f);
    }

    private static Icon CreatePausedIcon(Icon source)
    {
        using var bitmap = source.ToBitmap();
        using var gray = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(gray))
        using (var attributes = new ImageAttributes())
        {
            // Desaturate every pixel and scale its alpha down, so the white ring turns translucent and the
            // paused icon reads like the sidebar's grayed brand mark instead of a solid gray disc. Each row
            // is one output channel, so the luminance weights sit in the column of the source channel.
            const float red = 0.299f;
            const float green = 0.587f;
            const float blue = 0.114f;
            attributes.SetColorMatrix(new ColorMatrix(new[]
            {
                new[] { red, red, red, 0f, 0f },
                new[] { green, green, green, 0f, 0f },
                new[] { blue, blue, blue, 0f, 0f },
                new[] { 0f, 0f, 0f, PausedIconOpacity, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f }
            }));
            graphics.DrawImage(
                bitmap,
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                0,
                0,
                bitmap.Width,
                bitmap.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        return ToIcon(gray);
    }

    private static Icon ToIcon(Bitmap bitmap)
    {
        // 用单帧 PNG ICO 流加载，而不是 Bitmap.GetHicon：后者会把半透明像素预乘，
        // 54% 不透明的托盘图标会被再乘一次，显示成接近全黑的灰块。
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);
        var pngBytes = png.ToArray();

        using var ico = new MemoryStream();
        using (var writer = new BinaryWriter(ico, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)1);
            writer.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
            writer.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(pngBytes.Length);
            writer.Write(22);
            writer.Write(pngBytes);
            writer.Flush();
        }

        ico.Position = 0;
        return new Icon(ico);
    }
}
