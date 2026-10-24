using System.Diagnostics;
using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

/// <summary>
/// Handles the three "pick an application" WebView messages: the file dialog, the click-a-window picker and
/// revealing the application folder in Explorer. It only needs the current config for theming and two
/// callbacks for replies, so the form keeps owning the window lifecycle.
/// </summary>
internal sealed class ApplicationPickerCoordinator
{
    private readonly Func<LoadedGestureConfig?> getConfig;
    private readonly Action<string> postJson;
    private readonly Action<bool, string, string> postConfigResult;
    private readonly Func<IWin32Window?> getDialogOwner;

    public ApplicationPickerCoordinator(
        Func<LoadedGestureConfig?> getConfig,
        Action<string> postJson,
        Action<bool, string, string> postConfigResult,
        Func<IWin32Window?> getDialogOwner)
    {
        this.getConfig = getConfig;
        this.postJson = postJson;
        this.postConfigResult = postConfigResult;
        this.getDialogOwner = getDialogOwner;
    }

    public void PickWindow(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SelectApplicationWebMessage>(json, WebMessageJson.Options);
            using var picker = new ApplicationTargetPickerForm(GetCursorColor());
            var result = picker.ShowDialog(getDialogOwner());
            if (result == DialogResult.Abort)
            {
                postConfigResult(false, picker.ErrorMessage, "pick-application-window");
                return;
            }

            if (result != DialogResult.OK || picker.PickedApplication is null)
            {
                return;
            }

            PostApplicationSelected(
                message?.RequestId ?? "",
                picker.PickedApplication.Name,
                picker.PickedApplication.Name,
                picker.PickedApplication.Path,
                message?.Category ?? "");
        }
        catch (Exception exception)
        {
            postConfigResult(false, exception.Message, "pick-application-window");
        }
    }

    public void Select(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SelectApplicationWebMessage>(json, WebMessageJson.Options);
            using var dialog = new OpenFileDialog
            {
                Title = "选择应用程序",
                Filter = "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(getDialogOwner()) != DialogResult.OK)
            {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(dialog.FileName);
            PostApplicationSelected(
                message?.RequestId ?? "",
                name,
                name,
                dialog.FileName,
                message?.Category ?? "");
        }
        catch (Exception exception)
        {
            postConfigResult(false, exception.Message, "select-application");
        }
    }

    public void OpenFolder(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<OpenApplicationFolderWebMessage>(json, WebMessageJson.Options);
            var directory = Path.GetDirectoryName(message?.Path ?? "");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            _ = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true,
                Arguments = directory
            });
        }
        catch (Exception exception)
        {
            AppLogger.Error(
                "ApplicationPickerCoordinator",
                "open-application-folder-failed",
                "Failed to open application folder.",
                exception);
        }
    }

    /// <summary>
    /// The picker draws its own cursor crosshair over an arbitrary window, so it cannot rely on the form
    /// theme; resolve the effective theme (explicit setting, otherwise the Windows preference).
    /// </summary>
    private Color GetCursorColor()
    {
        var theme = getConfig()?.Config.UiSettings.Appearance.Theme;
        var isDarkTheme = theme == GestureConfigContract.AppearanceThemes.Dark ||
            (theme != GestureConfigContract.AppearanceThemes.Light && SystemTheme.PrefersDarkMode());

        return isDarkTheme
            ? Color.FromArgb(0xED, 0xF2, 0xF7)
            : Color.FromArgb(0x16, 0x20, 0x2B);
    }

    private void PostApplicationSelected(string requestId, string name, string displayName, string path, string category)
    {
        postJson(JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.ApplicationSelected,
            requestId,
            name,
            displayName,
            path,
            category,
            icon = ApplicationIconDataUrl.FromExecutable(path)
        }));
    }
}
