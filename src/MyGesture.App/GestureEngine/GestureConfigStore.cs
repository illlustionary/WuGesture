using System.Text.Json;
using MyGesture.App;

namespace MyGesture.App.GestureEngine;

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
        config.UiSettings.AppBehavior ??= new AppBehaviorUiSettings();
        config.UiSettings.WebDav ??= new WebDavUiSettings();
        NormalizeUiSettings(config.UiSettings);
        return config;
    }

    private static void NormalizeUiSettings(GestureUiSettings settings)
    {
        var mouseTrail = settings.MouseTrail;
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
        gestureHint.WidthPercent = ClampInteger(gestureHint.WidthPercent, 10, 90, 28);
        gestureHint.HeightPercent = ClampInteger(gestureHint.HeightPercent, 5, 40, 11);
        gestureHint.BottomOffsetPercent = ClampInteger(gestureHint.BottomOffsetPercent, 0, 100, 13);
        if (gestureHint.BottomOffset < 0)
        {
            gestureHint.BottomOffset = 140;
        }

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
