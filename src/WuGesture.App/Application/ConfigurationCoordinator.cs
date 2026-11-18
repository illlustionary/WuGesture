using System.Text.Json;
using System.Text.Json.Nodes;
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
    private string? appliedStartMode;

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
        var loaded = configStore.LoadOrCreate();
        appliedStartMode = loaded.Config.UiSettings.AppBehavior.StartMode;
        return loaded;
    }

    /// <summary>
    /// Applies a configuration that is already persisted. Switching to or away from the "skip UAC"
    /// start mode also registers or removes the scheduled task, which is the only step that may prompt
    /// for administrator rights. A failure there is reported but never blocks the rest of the config.
    /// </summary>
    private string? ApplyConfig(LoadedGestureConfig loaded)
    {
        var nextStartMode = loaded.Config.UiSettings.AppBehavior.StartMode;
        string? taskFailure = null;
        try
        {
            SyncScheduledTask(appliedStartMode, nextStartMode);
        }
        catch (InvalidOperationException exception)
        {
            taskFailure = exception.Message;
            AppLogger.Warning(
                "ConfigurationCoordinator",
                "start-mode-sync-failed",
                exception.Message,
                exception);
        }

        appliedStartMode = nextStartMode;
        applyConfig(loaded);
        return taskFailure;
    }

    private void PostConfigApplied(string successMessage, string? taskFailure, string operation)
    {
        postConfigResult(
            taskFailure is null,
            taskFailure is null
                ? successMessage
                : $"{successMessage.TrimEnd('。')}，但启动模式未能生效：{taskFailure}",
            operation);
    }

    private static void SyncScheduledTask(string? previous, string next)
    {
        if (previous == next)
        {
            return;
        }

        if (next == GestureConfigContract.AppStartModes.SkipUac)
        {
            if (!ScheduledTaskLauncher.TryRegister(out var failure))
            {
                throw new InvalidOperationException($"无法创建以最高权限启动的计划任务：{failure}");
            }

            return;
        }

        if (previous == GestureConfigContract.AppStartModes.SkipUac &&
            !ScheduledTaskLauncher.TryUnregister(out var removeFailure))
        {
            throw new InvalidOperationException($"无法删除以最高权限启动的计划任务：{removeFailure}");
        }
    }

    public void Save(string json)
    {
        try
        {
            var taskFailure = ApplyConfig(configStore.SaveAndLoad(ReadConfig(json)));
            postRules();
            PostConfigApplied("配置已保存。", taskFailure, "save");
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
            var taskFailure = ApplyConfig(configStore.LoadOrCreate());
            postRules();
            PostConfigApplied("本地配置已重新加载。", taskFailure, "reload");
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
            var taskFailure = ApplyConfig(configStore.ResetToDefaults());
            postRules();
            PostConfigApplied("默认配置已恢复。", taskFailure, "reset");
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
            var taskFailure = ApplyConfig(saved);

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

            File.WriteAllText(dialog.FileName, BuildExportDocument(File.ReadAllText(saved.FilePath)));
            postRules();
            PostConfigApplied("配置已导出（不含 WebDAV 口令）。", taskFailure, "export");
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

            var confirm = MessageBox.Show(
                getDialogOwner(),
                $"将使用“{Path.GetFileName(dialog.FileName)}”覆盖当前配置，导入后无法撤销。是否继续？",
                "导入配置",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var taskFailure = ApplyConfig(configStore.SaveJsonAndLoad(File.ReadAllText(dialog.FileName)));
            postRules();
            PostConfigApplied("配置已导入。", taskFailure, "import");
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
            var taskFailure = ApplyConfig(saved);
            localConfigSaved = true;
            await webDavConfigSyncService.UploadAsync(saved.FilePath, saved.Config.UiSettings.WebDav);

            postRules();
            if (taskFailure is null)
            {
                postWebDavResult("save", true, "当前配置已备份到 WebDAV。");
            }
            else
            {
                postWebDavResult("save", false, $"当前配置已备份到 WebDAV，但启动模式未能生效：{taskFailure}");
            }
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
            var taskFailure = ApplyConfig(configStore.SaveJsonAndLoad(downloadedJson, settings));

            postRules();
            if (taskFailure is null)
            {
                postWebDavResult("restore", true, "已从 WebDAV 恢复本地配置。");
            }
            else
            {
                postWebDavResult("restore", false, $"已从 WebDAV 恢复本地配置，但启动模式未能生效：{taskFailure}");
            }
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

    /// <summary>
    /// Exported files must not contain the WebDAV password: exports are often copied to shared folders or
    /// attached to bug reports. The local configuration keeps the credential.
    /// </summary>
    internal static string BuildExportDocument(string json)
    {
        try
        {
            if (JsonNode.Parse(json) is not JsonObject root)
            {
                return json;
            }

            if (root["uiSettings"] is JsonObject uiSettings &&
                uiSettings["webDav"] is JsonObject webDav)
            {
                webDav["password"] = "";
            }

            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
