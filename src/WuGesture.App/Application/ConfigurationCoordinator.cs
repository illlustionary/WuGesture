using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

/// <summary>
/// Owns every path that replaces the local configuration: save, reload, reset, local import/export and
/// WebDAV backup/restore. The form supplies the current config, the "apply" step and the WebView replies,
/// so this type stays free of window lifecycle concerns.
/// </summary>
internal sealed class ConfigurationCoordinator
{
    private readonly GestureConfigStore configStore = new();
    private readonly WebDavConfigSyncService webDavConfigSyncService = new();
    private readonly Func<LoadedGestureConfig?> getConfig;
    private readonly Action<LoadedGestureConfig> applyConfig;
    private readonly Action postRules;
    private readonly Action<bool, string, string> postConfigResult;
    private readonly Action<string, bool, string> postWebDavResult;
    private readonly Func<IWin32Window?> getDialogOwner;

    public ConfigurationCoordinator(
        Func<LoadedGestureConfig?> getConfig,
        Action<LoadedGestureConfig> applyConfig,
        Action postRules,
        Action<bool, string, string> postConfigResult,
        Action<string, bool, string> postWebDavResult,
        Func<IWin32Window?> getDialogOwner)
    {
        this.getConfig = getConfig;
        this.applyConfig = applyConfig;
        this.postRules = postRules;
        this.postConfigResult = postConfigResult;
        this.postWebDavResult = postWebDavResult;
        this.getDialogOwner = getDialogOwner;
    }

    /// <summary>Loads (or creates) the local configuration during startup.</summary>
    public LoadedGestureConfig Load()
    {
        return configStore.LoadOrCreate();
    }

    public void Save(string json)
    {
        try
        {
            applyConfig(configStore.SaveAndLoad(ReadConfig(json)));
            postRules();
            postConfigResult(true, "配置已保存。", "save");
        }
        catch (Exception exception)
        {
            postConfigResult(false, $"保存配置失败：{exception.Message}", "save");
        }
    }

    public void Reload()
    {
        try
        {
            applyConfig(configStore.LoadOrCreate());
            postRules();
            postConfigResult(true, "本地配置已重新加载。", "reload");
        }
        catch (Exception exception)
        {
            postConfigResult(false, $"重新加载本地配置失败：{exception.Message}", "reload");
        }
    }

    public void Reset()
    {
        try
        {
            applyConfig(configStore.ResetToDefaults());
            postRules();
            postConfigResult(true, "默认配置已恢复。", "reset");
        }
        catch (Exception exception)
        {
            postConfigResult(false, $"恢复默认配置失败：{exception.Message}", "reset");
        }
    }

    public void Export(string json)
    {
        try
        {
            var saved = configStore.SaveAndLoad(ReadConfig(json));
            applyConfig(saved);

            using var dialog = new SaveFileDialog
            {
                Title = "导出配置",
                Filter = "JSON 配置 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = ConfigStorageContract.ConfigFileName,
                DefaultExt = "json",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(getDialogOwner()) != DialogResult.OK)
            {
                return;
            }

            File.WriteAllText(dialog.FileName, File.ReadAllText(saved.FilePath));
            postRules();
            postConfigResult(true, "配置已导出。", "export");
        }
        catch (Exception exception)
        {
            postConfigResult(false, $"导出配置失败：{exception.Message}", "export");
        }
    }

    public void Import()
    {
        try
        {
            using var dialog = new OpenFileDialog
            {
                Title = "导入配置",
                Filter = "JSON 配置 (*.json)|*.json|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(getDialogOwner()) != DialogResult.OK)
            {
                return;
            }

            applyConfig(configStore.SaveJsonAndLoad(File.ReadAllText(dialog.FileName)));
            postRules();
            postConfigResult(true, "配置已导入。", "import");
        }
        catch (Exception exception)
        {
            postConfigResult(false, $"导入配置失败：{exception.Message}", "import");
        }
    }

    public async void TestWebDavConnection(string json)
    {
        try
        {
            var settings = ReadWebDavSettings(json);
            await webDavConfigSyncService.TestConnectionAsync(settings);
            postWebDavResult("test", true, "WebDAV 连接测试成功。可以备份或恢复配置。");
        }
        catch (Exception exception)
        {
            postWebDavResult("test", false, $"WebDAV 连接测试失败：{exception.Message}");
        }
    }

    public async void SaveToWebDav(string json)
    {
        var localConfigSaved = false;

        try
        {
            var saved = configStore.SaveAndLoad(ReadConfig(json));
            applyConfig(saved);
            localConfigSaved = true;
            await webDavConfigSyncService.UploadAsync(saved.FilePath, saved.Config.UiSettings.WebDav);

            postRules();
            postWebDavResult("save", true, "当前配置已备份到 WebDAV。");
        }
        catch (Exception exception)
        {
            postWebDavResult(
                "save",
                false,
                localConfigSaved
                    ? $"本地配置已保存，但备份到 WebDAV 失败：{exception.Message}"
                    : $"备份到 WebDAV 失败：{exception.Message}");
        }
    }

    public async void RestoreFromWebDav(string json)
    {
        try
        {
            var settings = ReadWebDavSettings(json);
            var downloadedJson = await webDavConfigSyncService.DownloadAsync(settings);
            applyConfig(configStore.SaveJsonAndLoad(downloadedJson, settings));

            postRules();
            postWebDavResult("restore", true, "已从 WebDAV 恢复本地配置。");
        }
        catch (Exception exception)
        {
            postWebDavResult("restore", false, $"从 WebDAV 恢复配置失败：{exception.Message}");
        }
    }

    /// <summary>
    /// Rebuilds the whole configuration from a rules message. The editor always sends every section, so a
    /// missing section means "empty" instead of "keep the previous value".
    /// </summary>
    private GestureConfig ReadConfig(string json)
    {
        return CreateConfig(json, getConfig()?.Config.UiSettings);
    }

    internal static GestureConfig CreateConfig(string json, GestureUiSettings? fallbackUiSettings)
    {
        var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJson.Options);
        return new GestureConfig
        {
            SchemaVersion = GestureConfigContract.Schema.CurrentVersion,
            Rules = message?.Rules ?? [],
            Applications = message?.Applications ?? [],
            Categories = message?.Categories ?? [],
            EdgeActions = message?.EdgeActions ?? [],
            UiSettings = message?.UiSettings ?? fallbackUiSettings ?? new GestureUiSettings()
        };
    }

    private WebDavUiSettings ReadWebDavSettings(string json)
    {
        var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJson.Options);
        return message?.UiSettings?.WebDav ??
            getConfig()?.Config.UiSettings.WebDav ??
            new WebDavUiSettings();
    }
}
