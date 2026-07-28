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
        var config = GestureConfigNormalizer.Normalize(JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions));

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
        config = GestureConfigNormalizer.Normalize(config);
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
        var config = GestureConfigNormalizer.Normalize(JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions));
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
        config = GestureConfigNormalizer.Normalize(config);
        var directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    private static bool IsEmptyWebDavSettings(WebDavUiSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.Address) &&
            string.IsNullOrWhiteSpace(settings.UserName) &&
            string.IsNullOrWhiteSpace(settings.Password) &&
            string.IsNullOrWhiteSpace(settings.RemotePath);
    }

}
