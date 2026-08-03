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
        if (!TryReadSchemaVersion(json, out var schemaVersion, out var invalidReason))
        {
            return BackupAndReset(invalidReason);
        }

        if (schemaVersion > GestureConfigContract.Schema.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"配置版本 {schemaVersion} 高于当前支持版本 {GestureConfigContract.Schema.CurrentVersion}，请使用较新版本打开后再导出。");
        }

        if (schemaVersion < GestureConfigContract.Schema.CurrentVersion)
        {
            return BackupAndReset($"旧配置版本 {schemaVersion}");
        }

        GestureConfig? deserializedConfig;
        try
        {
            deserializedConfig = JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return BackupAndReset("配置 JSON 损坏");
        }

        if (deserializedConfig is null)
        {
            return BackupAndReset("配置内容为空");
        }

        var config = GestureConfigNormalizer.Normalize(deserializedConfig);

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
        EnsureCurrentSchema(config);
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
        var config = JsonSerializer.Deserialize<GestureConfig>(json, JsonOptions)
            ?? throw new InvalidOperationException("配置内容为空。");
        EnsureCurrentSchema(config);
        config = GestureConfigNormalizer.Normalize(config);
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
        EnsureCurrentSchema(config);
        config = GestureConfigNormalizer.Normalize(config);
        var directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    private LoadedGestureConfig BackupAndReset(string reason)
    {
        var backupPath = CreateBackupPath();
        File.Copy(ConfigPath, backupPath);

        var defaultConfig = DefaultGestureConfig.Create();
        Save(defaultConfig);
        AppLogger.Warning(
            "GestureConfigStore",
            "UnsupportedConfigReset",
            $"{reason}，已备份为 {Path.GetFileName(backupPath)} 并重置为默认配置。");
        return new LoadedGestureConfig(ConfigPath, defaultConfig, GestureConfigMapper.ToRules(defaultConfig));
    }

    private static bool TryReadSchemaVersion(string json, out int schemaVersion, out string reason)
    {
        schemaVersion = 0;
        reason = "配置缺少当前版本标记";

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                reason = "配置根节点不是对象";
                return false;
            }

            if (!document.RootElement.TryGetProperty("schemaVersion", out var versionElement) ||
                versionElement.ValueKind != JsonValueKind.Number ||
                !versionElement.TryGetInt32(out schemaVersion))
            {
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            reason = "配置 JSON 损坏";
            return false;
        }
    }

    private static void EnsureCurrentSchema(GestureConfig config)
    {
        if (config.SchemaVersion != GestureConfigContract.Schema.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"仅支持配置版本 {GestureConfigContract.Schema.CurrentVersion}，当前配置版本为 {config.SchemaVersion}。");
        }
    }

    private string CreateBackupPath()
    {
        var directory = Path.GetDirectoryName(ConfigPath) ?? Directory.GetCurrentDirectory();
        var fileName = Path.GetFileNameWithoutExtension(ConfigPath);
        var extension = Path.GetExtension(ConfigPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");

        for (var suffix = 0; suffix < 100; suffix++)
        {
            var suffixText = suffix == 0 ? "" : $"-{suffix}";
            var candidate = Path.Combine(directory, $"{fileName}.backup-{timestamp}{suffixText}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("无法为旧配置创建唯一备份文件名。");
    }

    private static bool IsEmptyWebDavSettings(WebDavUiSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.Address) &&
            string.IsNullOrWhiteSpace(settings.UserName) &&
            string.IsNullOrWhiteSpace(settings.Password) &&
            string.IsNullOrWhiteSpace(settings.RemotePath);
    }

}
