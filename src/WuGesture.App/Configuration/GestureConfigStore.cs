using System.Text.Json;
using System.Text.Json.Nodes;
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

        if (schemaVersion is > GestureConfigContract.Schema.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"配置版本 {schemaVersion} 高于当前支持版本 {GestureConfigContract.Schema.CurrentVersion}，请使用较新版本打开后再导出。");
        }

        GestureConfig config;
        try
        {
            config = DeserializeWithDefaults(json);
        }
        catch (JsonException)
        {
            return BackupAndReset("配置 JSON 损坏");
        }

        if (schemaVersion is null || schemaVersion < GestureConfigContract.Schema.CurrentVersion)
        {
            return BackupAndMigrate(config, invalidReason);
        }

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
        if (!TryReadSchemaVersion(json, out var schemaVersion, out var invalidReason))
        {
            throw new InvalidOperationException(invalidReason);
        }

        if (schemaVersion is > GestureConfigContract.Schema.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"配置版本 {schemaVersion} 高于当前支持版本 {GestureConfigContract.Schema.CurrentVersion}，请使用较新版本打开后再导出。");
        }

        var config = DeserializeWithDefaults(json);
        config.SchemaVersion = GestureConfigContract.Schema.CurrentVersion;
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

    private LoadedGestureConfig BackupAndMigrate(GestureConfig config, string reason)
    {
        var backupPath = CreateBackupPath();
        File.Copy(ConfigPath, backupPath);

        config.SchemaVersion = GestureConfigContract.Schema.CurrentVersion;
        Save(config);
        AppLogger.Information(
            "GestureConfigStore",
            "LegacyConfigMigrated",
            $"{reason}，已备份为 {Path.GetFileName(backupPath)} 并自动迁移到当前配置版本。");
        return new LoadedGestureConfig(ConfigPath, config, GestureConfigMapper.ToRules(config));
    }

    private static bool TryReadSchemaVersion(string json, out int? schemaVersion, out string reason)
    {
        schemaVersion = null;
        reason = "配置缺少版本标记";

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                reason = "配置根节点不是对象";
                return false;
            }

            if (!document.RootElement.TryGetProperty("schemaVersion", out var versionElement))
            {
                return true;
            }

            if (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var version))
            {
                reason = "配置版本标记无效";
                return true;
            }

            schemaVersion = version;
            return true;
        }
        catch (JsonException)
        {
            reason = "配置 JSON 损坏";
            return false;
        }
    }

    private static GestureConfig DeserializeWithDefaults(string json)
    {
        var source = JsonNode.Parse(json) as JsonObject
            ?? throw new JsonException("配置根节点不是对象。");
        var defaults = JsonSerializer.SerializeToNode(DefaultGestureConfig.Create(), JsonOptions)
            ?? throw new InvalidOperationException("无法创建默认配置。");
        var merged = MergeWithDefaults(defaults, source);
        var config = merged.Deserialize<GestureConfig>(JsonOptions)
            ?? throw new JsonException("配置内容为空。");
        return GestureConfigNormalizer.Normalize(config);
    }

    private static JsonNode MergeWithDefaults(JsonNode defaults, JsonNode? source)
    {
        if (source is null)
        {
            return defaults.DeepClone();
        }

        if (defaults is JsonObject defaultObject && source is JsonObject sourceObject)
        {
            var result = new JsonObject();
            foreach (var property in defaultObject)
            {
                sourceObject.TryGetPropertyValue(property.Key, out var sourceValue);
                result[property.Key] = MergeWithDefaults(property.Value!, sourceValue);
            }

            return result;
        }

        if (defaults is JsonArray && source is JsonArray)
        {
            return source.DeepClone();
        }

        return IsCompatibleValue(defaults, source) ? source.DeepClone() : defaults.DeepClone();
    }

    private static bool IsCompatibleValue(JsonNode defaults, JsonNode source)
    {
        using var defaultDocument = JsonDocument.Parse(defaults.ToJsonString());
        using var sourceDocument = JsonDocument.Parse(source.ToJsonString());
        var defaultKind = defaultDocument.RootElement.ValueKind;
        var sourceKind = sourceDocument.RootElement.ValueKind;

        return defaultKind == sourceKind ||
            ((defaultKind is JsonValueKind.True or JsonValueKind.False) &&
             (sourceKind is JsonValueKind.True or JsonValueKind.False));
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
