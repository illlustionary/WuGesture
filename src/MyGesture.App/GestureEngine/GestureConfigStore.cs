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

    public GestureConfigStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        ConfigPath = Path.Combine(appData, "MyGesture", "gestures.json");
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
        var config = JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions) ?? new GestureConfig();
        var rules = GestureConfigMapper.ToRules(config);

        if (config.Rules.Count == 0)
        {
            config = GestureConfigMapper.FromRules(DefaultGestureRules.Create());
            Save(config);
            rules = GestureConfigMapper.ToRules(config);
        }

        return new LoadedGestureConfig(ConfigPath, config, rules);
    }

    public LoadedGestureConfig SaveAndLoad(GestureConfig config)
    {
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
        var directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }
}
