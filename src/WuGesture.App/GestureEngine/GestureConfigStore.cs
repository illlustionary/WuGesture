using System.Text.Json;
using WuGesture.App;

namespace WuGesture.App.GestureEngine;

public sealed class GestureConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string ConfigPath { get; }

    public GestureConfigStore(string? configPath = null)
    {
        ConfigPath = configPath ?? GetDefaultConfigPath();
    }

    private static string GetDefaultConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppIdentity.AppDataFolderName, ConfigStorageContract.ConfigFileName);
    }

    public LoadedGestureConfig LoadOrCreate()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = DefaultGestureConfig.Create();
            Save(defaultConfig);
            return new LoadedGestureConfig(ConfigPath, defaultConfig, GestureConfigMapper.ToRules(defaultConfig));
        }

        var json = File.ReadAllText(ConfigPath);
        var config = NormalizeConfig(JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions));

        if (config.Rules.Count == 0)
        {
            var defaultConfig = DefaultGestureConfig.Create();
            config.Rules = defaultConfig.Rules;
            Save(config);
        }

        return new LoadedGestureConfig(ConfigPath, config, GestureConfigMapper.ToRules(config));
    }

    public LoadedGestureConfig SaveAndLoad(GestureConfig config)
    {
        config = NormalizeConfig(config);
        var rules = GestureConfigMapper.ToRules(config);
        if (config.Rules.Count == 0)
        {
            throw new InvalidOperationException("规则配置无效，未保存。");
        }

        Save(config);
        return new LoadedGestureConfig(ConfigPath, config, rules);
    }

    public LoadedGestureConfig SaveJsonAndLoad(string json, WebDavUiSettings? fallbackWebDavSettings = null)
    {
        var config = NormalizeConfig(JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions));
        if (fallbackWebDavSettings is not null && IsEmptyWebDavSettings(config.UiSettings.WebDav))
        {
            config.UiSettings.WebDav = fallbackWebDavSettings;
        }

        return SaveAndLoad(config);
    }

    public LoadedGestureConfig ResetToDefaults()
    {
        return SaveAndLoad(DefaultGestureConfig.Create());
    }

    public void Save(GestureConfig config)
    {
        config = NormalizeConfig(config);
        var directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    private static GestureConfig NormalizeConfig(GestureConfig? config)
    {
        config ??= new GestureConfig();
        config.Rules ??= [];
        config.Applications ??= [];
        config.EdgeActions ??= [];
        config.UiSettings ??= new GestureUiSettings();
        config.UiSettings.MouseTrail ??= new MouseTrailUiSettings();
        config.UiSettings.GestureHint ??= new GestureHintUiSettings();
        config.UiSettings.LevelOsd ??= new LevelOsdUiSettings();
        config.UiSettings.GestureSensitivity ??= new GestureSensitivityUiSettings();
        config.UiSettings.AppBehavior ??= new AppBehaviorUiSettings();
        config.UiSettings.WebDav ??= new WebDavUiSettings();
        NormalizeApplications(config.Applications);
        NormalizeUiSettings(config.UiSettings);
        return config;
    }

    private static void NormalizeApplications(IEnumerable<GestureApplicationConfig> applications)
    {
        foreach (var application in applications)
        {
            application.Categories ??= [];
            if (application.Categories.Count == 0 && !string.IsNullOrWhiteSpace(application.Category))
            {
                application.Categories.Add(application.Category);
            }

            application.Categories = application.Categories
                .Select(category => category.Trim())
                .Where(category => category.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            application.Category = null;
        }
    }

    private static void NormalizeUiSettings(GestureUiSettings settings)
    {
        var mouseTrail = settings.MouseTrail;
        mouseTrail.Enabled ??= true;
        var legacyThickness = mouseTrail.Thickness > 0 ? mouseTrail.Thickness : 3f;
        if (mouseTrail.InactiveThickness <= 0 ||
            (Math.Abs(mouseTrail.InactiveThickness - 3f) < 0.001f && Math.Abs(legacyThickness - 3f) > 0.001f))
        {
            mouseTrail.InactiveThickness = legacyThickness;
        }

        if (mouseTrail.ActiveThickness <= 0 ||
            (Math.Abs(mouseTrail.ActiveThickness - 3f) < 0.001f && Math.Abs(legacyThickness - 3f) > 0.001f))
        {
            mouseTrail.ActiveThickness = legacyThickness;
        }

        mouseTrail.Thickness = Math.Max(1f, mouseTrail.InactiveThickness);

        var gestureHint = settings.GestureHint;
        gestureHint.Enabled ??= true;
        gestureHint.DisplayDurationMs = ClampInteger(gestureHint.DisplayDurationMs, 300, 5000, 1800);
        gestureHint.FadeDurationMs = ClampInteger(gestureHint.FadeDurationMs, 0, 1000, 240);
        gestureHint.WidthPercent = ClampInteger(gestureHint.WidthPercent, 10, 90, 28);
        gestureHint.HeightPercent = ClampInteger(gestureHint.HeightPercent, 5, 40, 11);
        gestureHint.BottomOffsetPercent = ClampInteger(gestureHint.BottomOffsetPercent, 0, 100, 13);
        if (gestureHint.BottomOffset < 0)
        {
            gestureHint.BottomOffset = 140;
        }

        var levelOsd = settings.LevelOsd;
        levelOsd.Enabled ??= true;
        levelOsd.DisplayDurationMs = ClampInteger(levelOsd.DisplayDurationMs, 300, 5000, 1800);
        levelOsd.FadeDurationMs = ClampInteger(levelOsd.FadeDurationMs, 0, 1000, 240);
        levelOsd.BackgroundColor = NormalizeColor(levelOsd.BackgroundColor, "#28282C");
        levelOsd.BackgroundOpacity = ClampInteger(levelOsd.BackgroundOpacity, 0, 100, 88);
        levelOsd.TextColor = NormalizeColor(levelOsd.TextColor, "#DCDCDC");
        levelOsd.TrackColor = NormalizeColor(levelOsd.TrackColor, "#464646");
        levelOsd.VolumeColor = NormalizeColor(levelOsd.VolumeColor, "#64C8FF");
        levelOsd.BrightnessColor = NormalizeColor(levelOsd.BrightnessColor, "#FFC828");
        levelOsd.Width = ClampInteger(levelOsd.Width, 120, 480, 210);
        levelOsd.Height = ClampInteger(levelOsd.Height, 100, 420, 190);
        levelOsd.CornerRadius = ClampInteger(
            levelOsd.CornerRadius,
            0,
            Math.Min(levelOsd.Width, levelOsd.Height) / 2,
            22);
        levelOsd.Position = NormalizeLevelOsdPosition(levelOsd.Position);
        levelOsd.OffsetX = ClampInteger(levelOsd.OffsetX, -2000, 2000, 0);
        levelOsd.OffsetY = ClampInteger(levelOsd.OffsetY, -2000, 2000, 0);

        var gestureSensitivity = settings.GestureSensitivity;
        gestureSensitivity.Percent = ClampInteger(gestureSensitivity.Percent, 0, 200, 110);

        var appBehavior = settings.AppBehavior;
        appBehavior.CloseButtonBehavior = NormalizeCloseButtonBehavior(appBehavior.CloseButtonBehavior);
        appBehavior.ExcludedApplications = NormalizeExcludedApplications(appBehavior.ExcludedApplications);

        var webDav = settings.WebDav;
        webDav.Address = (webDav.Address ?? "").Trim();
        webDav.UserName = (webDav.UserName ?? "").Trim();
        webDav.Password ??= "";
        webDav.RemotePath = (webDav.RemotePath ?? "").Trim();
    }

    private static string NormalizeCloseButtonBehavior(string? value)
    {
        return value is
            GestureConfigContract.CloseButtonBehaviors.MinimizeToTray or
            GestureConfigContract.CloseButtonBehaviors.MinimizeToTaskbar or
            GestureConfigContract.CloseButtonBehaviors.Exit
            ? value
            : GestureConfigContract.CloseButtonBehaviors.MinimizeToTray;
    }

    private static string NormalizeLevelOsdPosition(string? value)
    {
        return value is
            GestureConfigContract.LevelOsdPositions.Center or
            GestureConfigContract.LevelOsdPositions.TopCenter or
            GestureConfigContract.LevelOsdPositions.BottomCenter or
            GestureConfigContract.LevelOsdPositions.TopLeft or
            GestureConfigContract.LevelOsdPositions.TopRight or
            GestureConfigContract.LevelOsdPositions.BottomLeft or
            GestureConfigContract.LevelOsdPositions.BottomRight
            ? value
            : GestureConfigContract.LevelOsdPositions.Center;
    }

    private static string NormalizeColor(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static List<ExcludedApplicationConfig> NormalizeExcludedApplications(IEnumerable<ExcludedApplicationConfig>? applications)
    {
        var result = new List<ExcludedApplicationConfig>();

        foreach (var application in applications ?? [])
        {
            var normalized = NormalizeExcludedApplication(application);
            if (string.IsNullOrWhiteSpace(normalized.Name) && string.IsNullOrWhiteSpace(normalized.Path))
            {
                continue;
            }

            if (!result.Any(existing => IsSameApplicationIdentity(existing, normalized)))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static ExcludedApplicationConfig NormalizeExcludedApplication(ExcludedApplicationConfig application)
    {
        var name = NormalizeAppName(application.Name);
        var path = (application.Path ?? "").Trim();
        var displayName = (application.DisplayName ?? "").Trim();

        return new ExcludedApplicationConfig
        {
            Name = name,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName,
            Path = path,
            DisableEdgeActions = application.DisableEdgeActions
        };
    }

    private static bool IsSameApplicationIdentity(ExcludedApplicationConfig left, ExcludedApplicationConfig right)
    {
        if (!string.IsNullOrWhiteSpace(left.Path) && !string.IsNullOrWhiteSpace(right.Path))
        {
            return string.Equals(left.Path.Trim(), right.Path.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            NormalizeAppName(left.Name),
            NormalizeAppName(right.Name),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAppName(string? value)
    {
        var trimmed = (value ?? "").Trim();
        var name = Path.GetFileNameWithoutExtension(trimmed);
        return string.IsNullOrWhiteSpace(name) ? trimmed : name;
    }

    private static bool IsEmptyWebDavSettings(WebDavUiSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.Address) &&
            string.IsNullOrWhiteSpace(settings.UserName) &&
            string.IsNullOrWhiteSpace(settings.Password) &&
            string.IsNullOrWhiteSpace(settings.RemotePath);
    }

    private static int ClampInteger(int value, int min, int max, int fallback)
    {
        if (value < min || value > max)
        {
            return fallback;
        }

        return value;
    }
}
