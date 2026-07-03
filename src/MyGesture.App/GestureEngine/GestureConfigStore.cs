using System.Text.Json;

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
        return Path.Combine(appData, "MyGesture", "gestures.json");
    }

    public LoadedGestureConfig LoadOrCreate()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = GestureConfigMapper.FromRules(DefaultGestureRules.Create());
            Save(defaultConfig);
            return new LoadedGestureConfig(ConfigPath, defaultConfig, GestureConfigMapper.ToRules(defaultConfig));
        }

        var json = File.ReadAllText(ConfigPath);
        var config = NormalizeConfig(JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions));

        if (config.Rules.Count == 0)
        {
            var defaultConfig = GestureConfigMapper.FromRules(DefaultGestureRules.Create());
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

    public LoadedGestureConfig ResetToDefaults()
    {
        return SaveAndLoad(GestureConfigMapper.FromRules(DefaultGestureRules.Create()));
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
