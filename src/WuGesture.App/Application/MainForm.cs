using Microsoft.Win32;
using WuGesture.App.GestureEngine;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;

namespace WuGesture.App;

public sealed partial class MainForm : Form
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly GestureConfigStore configStore = new();
    private readonly WebDavConfigSyncService webDavConfigSyncService = new();
    private readonly KeyboardShortcutRecorder hotkeyRecorder = new();
    private readonly WindowStateStore windowStateStore = new();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly bool startHiddenToTray;
    private ToolStripMenuItem? pauseItem;
    private Icon? normalTrayIcon;
    private Icon? pausedTrayIcon;
    private readonly WebViewHost webViewHost;
    private ConfiguredScopeContextProvider? scopeContextProvider;
    private LoadedGestureConfig? loadedConfig;
    private GestureService? gestureService;
    private GestureFeedbackCoordinator? gestureFeedbackCoordinator;
    private EdgeActionService? edgeActionService;
    private MouseTrailForm? mouseTrailForm;
    private bool startMaximized;
    private bool isClosing;
    private bool isExiting;
    private bool isRestoringConfiguration;
    private bool removeTaskbarButtonAfterMinimize;
    private bool hideConfigWindowOnLaunch;
    private bool isUserPaused;
    private bool isConfigPaused;
    private bool isEditorPaused;
    private int gestureWindowCloseRequested;
    private int overlayRecoveryScheduled;

    private static readonly JsonSerializerOptions WebMessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MainForm(bool startHiddenToTray = false)
    {
        this.startHiddenToTray = startHiddenToTray;
        webViewHost = new WebViewHost(this, CanUseUi, HandleWebMessage);
        loadedConfig = configStore.LoadOrCreate();
        AppLogger.Information("MainForm", "config-loaded", "Loaded the local gesture configuration.");
        hideConfigWindowOnLaunch = startHiddenToTray ||
            !loadedConfig.Config.UiSettings.AppBehavior.ShowConfigWindowOnLaunch;
        Text = AppIdentity.DisplayName;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        FormBorderStyle = FormBorderStyle.Sizable;
        ApplyWindowTheme(loadedConfig.Config.UiSettings.Appearance);
        StartPosition = FormStartPosition.Manual;
        MinimumSize = new Size(WindowStateStore.MinimumWindowWidth, WindowStateStore.MinimumWindowHeight);
        ApplyInitialWindowState();
        Opacity = 0;
        if (hideConfigWindowOnLaunch)
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
        }

        InitializeTrayIcon();

        Load += OnLoad;
        Resize += OnResize;
        FormClosing += OnFormClosing;
        FormClosed += (_, _) =>
        {
            ActionExecutor.WindowCloseRequested -= OnGestureWindowCloseRequested;
            SystemEvents.UserPreferenceChanged -= OnSystemUserPreferenceChanged;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
            normalTrayIcon?.Dispose();
            pausedTrayIcon?.Dispose();
            DisposeWebView();
            LevelOsdOverlay.Reset();
            gestureFeedbackCoordinator?.Dispose();
            DisposeMouseTrailForm();
        };
        ActionExecutor.WindowCloseRequested += OnGestureWindowCloseRequested;
        SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        AppLogger.Information("MainForm", "initializing", $"Initializing services. Hidden startup: {hideConfigWindowOnLaunch}.");
        var config = loadedConfig ??= configStore.LoadOrCreate();
        if (hideConfigWindowOnLaunch)
        {
            HideStartupWindow();
        }

        if (startMaximized && !hideConfigWindowOnLaunch)
        {
            WindowState = FormWindowState.Maximized;
        }

        LevelOsdOverlay.Configure(
            SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext(),
            ShowLevelOsd);
        isConfigPaused = config.Config.UiSettings.AppBehavior.GesturePaused;
        ApplyAppBehaviorSettings(config.Config.UiSettings.AppBehavior);
        if (TryRelaunchAsAdministrator(config.Config.UiSettings.AppBehavior))
        {
            return;
        }

        scopeContextProvider = new ConfiguredScopeContextProvider(config.Config.Applications);
        gestureService = new GestureService(new GestureMatcher(config.Rules), scopeContextProvider);
        gestureService.UpdateExcludedApplications(config.Config.UiSettings.AppBehavior.ExcludedApplications);
        gestureService.UpdateFullscreenBehavior(config.Config.UiSettings.AppBehavior.DisableGesturesInFullscreen);
        gestureService.UpdateTargetWindowMode(config.Config.UiSettings.AppBehavior.TargetWindowMode);
        edgeActionService = new EdgeActionService(config.Config.EdgeActions);
        edgeActionService.UpdateExcludedApplications(config.Config.UiSettings.AppBehavior.ExcludedApplications);
        edgeActionService.UpdateFullscreenBehavior(config.Config.UiSettings.AppBehavior.DisableEdgeActionsInFullscreen);
        ApplyUiSettings(config.Config.UiSettings);

        if (!hideConfigWindowOnLaunch)
        {
            await EnsureWebViewAsync();
            if (!CanUseUi())
            {
                return;
            }

            Opacity = 1;
            BringWindowToFront();
        }

        if (!CanUseUi())
        {
            return;
        }

        if (IsFeatureEnabled(config.Config.UiSettings.GestureHint.Enabled) ||
            IsFeatureEnabled(config.Config.UiSettings.MouseTrail.Enabled) ||
            IsFeatureEnabled(config.Config.UiSettings.LevelOsd.Enabled))
        {
            EnsureMouseTrailForm().Preload();
        }

        gestureFeedbackCoordinator = new GestureFeedbackCoordinator(
            this,
            gestureService,
            () => loadedConfig?.Config.UiSettings,
            EnsureMouseTrailForm,
            () => mouseTrailForm,
            TryPostWebMessage,
            CanUseUi);
        edgeActionService.EdgeActionFailed += OnEdgeActionFailed;
        hotkeyRecorder.HotkeyRecorded += OnHotkeyRecorded;
        gestureService.Start();
        edgeActionService.Start();
        ApplyGesturePauseState();
        AppLogger.Information("MainForm", "services-started", "Gesture and edge-action services are running.");

    }

    private void InitializeTrayIcon()
    {
        var openItem = new ToolStripMenuItem("打开配置", null, (_, _) => _ = RestoreFromTrayAsync());
        pauseItem = new ToolStripMenuItem($"暂停 {AppIdentity.DisplayName}")
        {
            CheckOnClick = true
        };
        pauseItem.CheckedChanged += (_, _) => SetUserPaused(pauseItem.Checked);
        var exitItem = new ToolStripMenuItem("退出", null, (_, _) => ExitFromTray());
        trayMenu.Items.Add(openItem);
        trayMenu.Items.Add(pauseItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(exitItem);

        normalTrayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        pausedTrayIcon = CreateGrayscaleIcon(normalTrayIcon);
        trayIcon.Text = AppIdentity.DisplayName;
        trayIcon.Icon = normalTrayIcon;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => _ = RestoreFromTrayAsync();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        var isGestureWindowClose = e.CloseReason == CloseReason.TaskManagerClosing &&
            Interlocked.Exchange(ref gestureWindowCloseRequested, 0) == 1;
        if ((e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None || isGestureWindowClose) &&
            HandleConfiguredUserClose())
        {
            e.Cancel = true;
            return;
        }

        isClosing = true;
        AppLogger.Information("MainForm", "closing", $"Closing with reason: {e.CloseReason}.");
        SaveWindowState();
        hotkeyRecorder.Stop();
        gestureFeedbackCoordinator?.Dispose();
        gestureService?.Dispose();
        edgeActionService?.Dispose();
        hotkeyRecorder.Dispose();
        DisposeMouseTrailForm();
        DisposeWebView();
    }

    private void OnGestureWindowCloseRequested(IntPtr targetWindow)
    {
        if (IsHandleCreated && targetWindow == Handle)
        {
            Volatile.Write(ref gestureWindowCloseRequested, 1);
        }
    }

    private void OnResize(object? sender, EventArgs e)
    {
        if (!removeTaskbarButtonAfterMinimize || WindowState != FormWindowState.Minimized)
        {
            return;
        }

        removeTaskbarButtonAfterMinimize = false;
        BeginInvokeSafe(() =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                Hide();
                DisposeWebView();
            }
        });
    }

    private bool HandleConfiguredUserClose()
    {
        if (isExiting)
        {
            return false;
        }

        var closeButtonBehavior = GetCloseButtonBehavior();
        if (closeButtonBehavior == GestureConfigContract.CloseButtonBehaviors.Exit)
        {
            isExiting = true;
            return false;
        }

        if (closeButtonBehavior == GestureConfigContract.CloseButtonBehaviors.MinimizeToTaskbar)
        {
            MinimizeToTaskbar();
        }
        else
        {
            MinimizeToTray();
        }

        return true;
    }

    private void MinimizeToTray()
    {
        SaveWindowState();
        hotkeyRecorder.Stop();
        gestureService?.StopRecording();
        isEditorPaused = false;
        ApplyGesturePauseState();
        mouseTrailForm?.HideTrail();
        ShowInTaskbar = true;
        removeTaskbarButtonAfterMinimize = true;
        MinimizeWindow();
    }

    private void MinimizeToTaskbar()
    {
        SaveWindowState();
        hotkeyRecorder.Stop();
        gestureService?.StopRecording();
        isEditorPaused = false;
        ApplyGesturePauseState();
        mouseTrailForm?.HideTrail();
        removeTaskbarButtonAfterMinimize = false;
        ShowInTaskbar = true;
        MinimizeWindow();
    }

    private async Task RestoreFromTrayAsync()
    {
        if (isClosing || IsDisposed || isRestoringConfiguration)
        {
            return;
        }

        isRestoringConfiguration = true;
        try
        {
            removeTaskbarButtonAfterMinimize = false;
            ShowInTaskbar = false;
            Opacity = 0;
            ApplyWindowTheme(loadedConfig?.Config.UiSettings.Appearance ?? new AppearanceUiSettings());
            if (!Visible)
            {
                Show();
            }

            await EnsureWebViewAsync();
            if (!CanUseUi())
            {
                return;
            }

            ShowInTaskbar = true;
            Opacity = 1;

            // Run after the tray menu or single-instance callback has returned so it cannot reclaim focus.
            await BringWindowToFrontAsync();
        }
        finally
        {
            isRestoringConfiguration = false;
        }
    }

    private void HideStartupWindow()
    {
        ShowInTaskbar = false;
        Hide();
        Opacity = 1;
    }

    private void ExitFromTray()
    {
        isExiting = true;
        Close();
    }

    public async Task ShowExistingInstanceAsync()
    {
        if (Visible && WindowState != FormWindowState.Minimized)
        {
            BringWindowToFront(attachToForegroundInputFirst: true);
            return;
        }

        await RestoreFromTrayAsync();
    }

    private Task BringWindowToFrontAsync()
    {
        if (!CanUseUi())
        {
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            BeginInvoke(() =>
            {
                try
                {
                    if (CanUseUi())
                    {
                        BringWindowToFront();
                    }
                }
                finally
                {
                    completion.TrySetResult();
                }
            });
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }

        return completion.Task;
    }

    private void SetUserPaused(bool paused)
    {
        if (isUserPaused != paused)
        {
            isUserPaused = paused;
            if (pauseItem is not null && pauseItem.Checked != paused)
            {
                pauseItem.Checked = paused;
            }

            UpdatePauseMenuText();
            ApplyGesturePauseState();
        }

        PostStatus(isUserPaused ? "paused" : "running");
    }

    private void UpdatePauseMenuText()
    {
        if (pauseItem is not null)
        {
            pauseItem.Text = isUserPaused ? $"恢复 {AppIdentity.DisplayName}" : $"暂停 {AppIdentity.DisplayName}";
        }

        trayIcon.Text = isUserPaused ? $"{AppIdentity.DisplayName}（已暂停）" : AppIdentity.DisplayName;
        trayIcon.Icon = isUserPaused && pausedTrayIcon is not null
            ? pausedTrayIcon
            : normalTrayIcon;
    }

    private void ApplyGesturePauseState()
    {
        var paused = isUserPaused || isConfigPaused || isEditorPaused;
        gestureService?.SetPaused(paused);
        edgeActionService?.SetPaused(paused);
        if (paused)
        {
            mouseTrailForm?.HideTrail();
        }
    }

    private string GetCloseButtonBehavior()
    {
        return loadedConfig?.Config.UiSettings.AppBehavior.CloseButtonBehavior is
            GestureConfigContract.CloseButtonBehaviors.MinimizeToTray or
            GestureConfigContract.CloseButtonBehaviors.MinimizeToTaskbar or
            GestureConfigContract.CloseButtonBehaviors.Exit
            ? loadedConfig.Config.UiSettings.AppBehavior.CloseButtonBehavior
            : GestureConfigContract.CloseButtonBehaviors.MinimizeToTray;
    }

    private void ApplyAppBehaviorSettings(AppBehaviorUiSettings settings)
    {
        ApplyStartupRegistration(settings.LaunchAtStartup);
    }

    private static void ApplyStartupRegistration(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true) ??
                Registry.CurrentUser.CreateSubKey(StartupRegistryPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (enabled)
            {
                key.SetValue(AppIdentity.StartupRegistryValueName, $"\"{AppIdentity.GetLaunchExecutablePath()}\" {AppIdentity.StartupLaunchArgument}");
            }
            else
            {
                key.DeleteValue(AppIdentity.StartupRegistryValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
        }
    }

    private bool TryRelaunchAsAdministrator(AppBehaviorUiSettings settings)
    {
        if (!settings.RunAsAdministrator || IsRunningAsAdministrator())
        {
            return false;
        }

        try
        {
            using var elevatedProcess = Process.Start(new ProcessStartInfo
            {
                FileName = AppIdentity.GetLaunchExecutablePath(),
                Arguments = startHiddenToTray
                    ? $"{AppIdentity.ElevatedRelaunchArgument} {AppIdentity.StartupLaunchArgument}"
                    : AppIdentity.ElevatedRelaunchArgument,
                UseShellExecute = true,
                Verb = "runas"
            });
            var foregroundPermissionGranted = elevatedProcess is not null &&
                AllowSetForegroundWindow((uint)elevatedProcess.Id);
            AppLogger.Information(
                "MainForm",
                "elevated-relaunch-started",
                $"Started elevated process {elevatedProcess?.Id.ToString() ?? "unknown"}; foreground permission granted: {foregroundPermissionGranted}.");
            isExiting = true;
            BeginInvoke(new Action(Close));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private async Task EnsureWebViewAsync()
    {
        await webViewHost.EnsureAsync();
    }

    private void DisposeWebView()
    {
        webViewHost.Dispose();
    }

    private void TryPostWebMessage(string payload)
    {
        if (isClosing)
        {
            return;
        }

        webViewHost.TryPostJson(payload);
    }

    private void OnEdgeActionFailed(object? sender, EdgeActionFailedEventArgs e)
    {
        AppLogger.Error("MainForm", "edge-action-failed", $"Edge action failed: {e.ActionName}.", e.Exception);
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnEdgeActionFailed(sender, e));
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.EdgeActionFailed,
            action = e.ActionName,
            error = e.Exception.Message
        });

        TryPostWebMessage(payload);
    }

    private void OnHotkeyRecorded(object? sender, HotkeyRecordedEventArgs e)
    {
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnHotkeyRecorded(sender, e));
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.HotkeyRecorded,
            requestId = e.RequestId,
            keys = e.Keys
        });

        TryPostWebMessage(payload);
    }

    private void PostStatus(string status)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.Status,
            status
        });

        TryPostWebMessage(payload);
    }

    private void PostRules()
    {
        if (loadedConfig is null)
        {
            return;
        }

        TryPostWebMessage(WebViewRulesPayloadFactory.Create(loadedConfig));
    }

    private void HandleWebMessage(string json)
    {
        if (!CanUseUi())
        {
            return;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.String &&
            root.GetString() == WebViewMessageTypes.GetStatus)
        {
            PostStatus(isUserPaused ? "paused" : "running");
            PostRules();
            return;
        }

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("type", out var typeElement))
        {
            return;
        }

        switch (typeElement.GetString())
        {
            case WebViewMessageTypes.SelectApplication:
                SelectApplication(json);
                break;
            case WebViewMessageTypes.PickApplicationWindow:
                PickApplicationWindow(json);
                break;
            case WebViewMessageTypes.OpenApplicationFolder:
                OpenApplicationFolder(json);
                break;
            case WebViewMessageTypes.StartGestureRecording:
                StartGestureRecording(json);
                break;
            case WebViewMessageTypes.StopGestureRecording:
                StopGestureRecording();
                break;
            case WebViewMessageTypes.SetGesturePaused:
                SetGesturePaused(json);
                break;
            case WebViewMessageTypes.SetUserPaused:
                SetUserPaused(json);
                break;
            case WebViewMessageTypes.StartHotkeyRecording:
                StartHotkeyRecording(json);
                break;
            case WebViewMessageTypes.StopHotkeyRecording:
                hotkeyRecorder.Stop();
                break;
            case WebViewMessageTypes.SaveRules:
                SaveRules(json);
                break;
            case WebViewMessageTypes.WebDavTest:
                TestWebDavConnection(json);
                break;
            case WebViewMessageTypes.WebDavSave:
                SaveConfigToWebDav(json);
                break;
            case WebViewMessageTypes.WebDavRestore:
                RestoreConfigFromWebDav(json);
                break;
            case WebViewMessageTypes.ExportConfig:
                ExportConfig(json);
                break;
            case WebViewMessageTypes.ImportConfig:
                ImportConfig();
                break;
            case WebViewMessageTypes.ReloadRules:
                ReloadRules();
                break;
            case WebViewMessageTypes.ResetRules:
                ResetRules();
                break;
            case WebViewMessageTypes.PreviewLevelOsd:
                PreviewLevelOsd(json);
                break;
        }
    }

    private void PreviewLevelOsd(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<PreviewLevelOsdWebMessage>(json, WebMessageJsonOptions);
            if (string.Equals(message?.Kind, "brightness", StringComparison.OrdinalIgnoreCase))
            {
                LevelOsdOverlay.ShowBrightnessPreview(62);
            }
            else
            {
                LevelOsdOverlay.ShowVolumePreview(72);
            }
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void StartHotkeyRecording(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<StartHotkeyRecordingWebMessage>(json, WebMessageJsonOptions);
            hotkeyRecorder.Start(message?.RequestId ?? "");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void SetGesturePaused(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SetGesturePausedWebMessage>(json, WebMessageJsonOptions);
            isEditorPaused = message?.Paused ?? false;
            ApplyGesturePauseState();
            if (message?.Paused == true)
            {
                mouseTrailForm?.HideTrail();
            }
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void SetUserPaused(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SetGesturePausedWebMessage>(json, WebMessageJsonOptions);
            SetUserPaused(message?.Paused ?? false);
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void StartGestureRecording(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<StartGestureRecordingWebMessage>(json, WebMessageJsonOptions);
            gestureService?.StartRecording(message?.RequestId ?? "");
            mouseTrailForm?.HideTrail();
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void StopGestureRecording()
    {
        gestureService?.StopRecording();
        mouseTrailForm?.HideTrail();
    }

    private void PickApplicationWindow(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SelectApplicationWebMessage>(json, WebMessageJsonOptions);
            using var picker = new ApplicationTargetPickerForm(GetApplicationPickerCursorColor());
            var result = picker.ShowDialog(this);
            if (result == DialogResult.Abort)
            {
                PostConfigResult(false, picker.ErrorMessage);
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
            PostConfigResult(false, exception.Message);
        }
    }

    private Color GetApplicationPickerCursorColor()
    {
        var theme = loadedConfig?.Config.UiSettings.Appearance.Theme;
        var isDarkTheme = theme == GestureConfigContract.AppearanceThemes.Dark ||
            (theme != GestureConfigContract.AppearanceThemes.Light && SystemPrefersDarkTheme());

        return isDarkTheme
            ? Color.FromArgb(0xED, 0xF2, 0xF7)
            : Color.FromArgb(0x16, 0x20, 0x2B);
    }

    private static bool SystemPrefersDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    private void SelectApplication(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SelectApplicationWebMessage>(json, WebMessageJsonOptions);
            using var dialog = new OpenFileDialog
            {
                Title = "选择应用程序",
                Filter = "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            PostApplicationSelected(
                message?.RequestId ?? "",
                Path.GetFileNameWithoutExtension(dialog.FileName),
                Path.GetFileNameWithoutExtension(dialog.FileName),
                dialog.FileName,
                message?.Category ?? "");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void OpenApplicationFolder(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<OpenApplicationFolderWebMessage>(json, WebMessageJsonOptions);
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
            AppLogger.Error("MainForm", "open-application-folder-failed", "Failed to open application folder.", exception);
        }
    }

    private void PostApplicationSelected(string requestId, string name, string displayName, string path, string category)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.ApplicationSelected,
            requestId,
            name,
            displayName,
            path,
            category,
            icon = ApplicationIconDataUrl.FromExecutable(path)
        });

        TryPostWebMessage(payload);
    }

    private void SaveRules(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var uiSettings = message?.UiSettings ?? loadedConfig?.Config.UiSettings ?? new GestureUiSettings();
            var config = new GestureConfig
            {
                SchemaVersion = GestureConfigContract.Schema.CurrentVersion,
                Rules = message?.Rules ?? [],
                Applications = message?.Applications ?? [],
                Categories = message?.Categories ?? [],
                EdgeActions = message?.EdgeActions ?? [],
                UiSettings = uiSettings
            };

            loadedConfig = configStore.SaveAndLoad(config);
            ApplyLoadedConfig();

            PostRules();
            PostConfigResult(true, "配置已保存。", "save");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, $"保存配置失败：{exception.Message}", "save");
        }
    }

    private async void TestWebDavConnection(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var settings = message?.UiSettings?.WebDav ?? loadedConfig?.Config.UiSettings.WebDav ?? new WebDavUiSettings();
            await webDavConfigSyncService.TestConnectionAsync(settings);
            PostWebDavResult("test", true, "WebDAV 连接测试成功。可以备份或恢复配置。");
        }
        catch (Exception exception)
        {
            PostWebDavResult("test", false, $"WebDAV 连接测试失败：{exception.Message}");
        }
    }

    private async void SaveConfigToWebDav(string json)
    {
        var localConfigSaved = false;

        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var uiSettings = message?.UiSettings ?? loadedConfig?.Config.UiSettings ?? new GestureUiSettings();
            var config = new GestureConfig
            {
                SchemaVersion = GestureConfigContract.Schema.CurrentVersion,
                Rules = message?.Rules ?? [],
                Applications = message?.Applications ?? [],
                Categories = message?.Categories ?? [],
                EdgeActions = message?.EdgeActions ?? [],
                UiSettings = uiSettings
            };

            loadedConfig = configStore.SaveAndLoad(config);
            localConfigSaved = true;
            ApplyLoadedConfig();
            await webDavConfigSyncService.UploadAsync(loadedConfig.FilePath, loadedConfig.Config.UiSettings.WebDav);

            PostRules();
            PostWebDavResult("save", true, "当前配置已备份到 WebDAV。");
        }
        catch (Exception exception)
        {
            var message = localConfigSaved
                ? $"本地配置已保存，但备份到 WebDAV 失败：{exception.Message}"
                : $"备份到 WebDAV 失败：{exception.Message}";
            PostWebDavResult("save", false, message);
        }
    }

    private async void RestoreConfigFromWebDav(string json)
    {
        try
        {
            if (loadedConfig is null)
            {
                loadedConfig = configStore.LoadOrCreate();
            }

            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var currentWebDavSettings = message?.UiSettings?.WebDav ?? loadedConfig.Config.UiSettings.WebDav;
            var downloadedJson = await webDavConfigSyncService.DownloadAsync(currentWebDavSettings);
            loadedConfig = configStore.SaveJsonAndLoad(downloadedJson, currentWebDavSettings);
            ApplyLoadedConfig();

            PostRules();
            PostWebDavResult("restore", true, "已从 WebDAV 恢复本地配置。");
        }
        catch (Exception exception)
        {
            PostWebDavResult("restore", false, $"从 WebDAV 恢复配置失败：{exception.Message}");
        }
    }

    private void ExportConfig(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var uiSettings = message?.UiSettings ?? loadedConfig?.Config.UiSettings ?? new GestureUiSettings();
            var config = new GestureConfig
            {
                SchemaVersion = GestureConfigContract.Schema.CurrentVersion,
                Rules = message?.Rules ?? [],
                Applications = message?.Applications ?? [],
                Categories = message?.Categories ?? [],
                EdgeActions = message?.EdgeActions ?? [],
                UiSettings = uiSettings
            };

            loadedConfig = configStore.SaveAndLoad(config);
            ApplyLoadedConfig();

            using var dialog = new SaveFileDialog
            {
                Title = "导出配置",
                Filter = "JSON 配置 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = ConfigStorageContract.ConfigFileName,
                DefaultExt = "json",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var configJson = File.ReadAllText(loadedConfig.FilePath);
            File.WriteAllText(dialog.FileName, configJson);

            PostRules();
            PostConfigResult(true, "配置已导出。", "export");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, $"导出配置失败：{exception.Message}", "export");
        }
    }

    private void ImportConfig()
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

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var configJson = File.ReadAllText(dialog.FileName);
            loadedConfig = configStore.SaveJsonAndLoad(configJson);
            ApplyLoadedConfig();

            PostRules();
            PostConfigResult(true, "配置已导入。", "import");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, $"导入配置失败：{exception.Message}", "import");
        }
    }

    private void ReloadRules()
    {
        try
        {
            loadedConfig = configStore.LoadOrCreate();
            ApplyLoadedConfig();

            PostRules();
            PostConfigResult(true, "本地配置已重新加载。", "reload");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, $"重新加载本地配置失败：{exception.Message}", "reload");
        }
    }

    private void ResetRules()
    {
        try
        {
            loadedConfig = configStore.ResetToDefaults();
            ApplyLoadedConfig();

            PostRules();
            PostConfigResult(true, "默认配置已恢复。", "reset");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, $"恢复默认配置失败：{exception.Message}", "reset");
        }
    }

    private void ApplyLoadedConfig()
    {
        if (loadedConfig is null)
        {
            return;
        }

        scopeContextProvider?.UpdateApplications(loadedConfig.Config.Applications);
        gestureService?.UpdateMatcher(new GestureMatcher(loadedConfig.Rules));
        gestureService?.UpdateExcludedApplications(loadedConfig.Config.UiSettings.AppBehavior.ExcludedApplications);
        gestureService?.UpdateFullscreenBehavior(loadedConfig.Config.UiSettings.AppBehavior.DisableGesturesInFullscreen);
        gestureService?.UpdateTargetWindowMode(loadedConfig.Config.UiSettings.AppBehavior.TargetWindowMode);
        edgeActionService?.UpdateActions(loadedConfig.Config.EdgeActions);
        edgeActionService?.UpdateExcludedApplications(loadedConfig.Config.UiSettings.AppBehavior.ExcludedApplications);
        edgeActionService?.UpdateFullscreenBehavior(loadedConfig.Config.UiSettings.AppBehavior.DisableEdgeActionsInFullscreen);
        isConfigPaused = loadedConfig.Config.UiSettings.AppBehavior.GesturePaused;
        ApplyAppBehaviorSettings(loadedConfig.Config.UiSettings.AppBehavior);
        ApplyUiSettings(loadedConfig.Config.UiSettings);
        ApplyGesturePauseState();
    }

    private void PostConfigResult(bool success, string message, string operation = "action")
    {
        if (!success)
        {
            AppLogger.Warning("MainForm", "configuration-operation-failed", $"Configuration operation failed: {operation}.");
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.ConfigResult,
            success,
            message,
            operation
        });

        TryPostWebMessage(payload);
    }

    private void PostWebDavResult(string operation, bool success, string message)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.WebDavResult,
            operation,
            success,
            message
        });

        TryPostWebMessage(payload);
    }

    private void BeginInvokeSafe(Action action)
    {
        if (!CanUseUi())
        {
            return;
        }

        BeginInvoke(() =>
        {
            if (CanUseUi())
            {
                action();
            }
        });
    }

    private bool CanUseUi()
    {
        return !isClosing && !IsDisposed && !Disposing && IsHandleCreated;
    }

    private static Icon CreateGrayscaleIcon(Icon source)
    {
        using var bitmap = source.ToBitmap();
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                var gray = (int)Math.Round(color.R * 0.299 + color.G * 0.587 + color.B * 0.114);
                bitmap.SetPixel(x, y, Color.FromArgb(color.A, gray, gray, gray));
            }
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private MouseTrailForm EnsureMouseTrailForm()
    {
        if (mouseTrailForm is { IsDisposed: false })
        {
            return mouseTrailForm;
        }

        mouseTrailForm?.Dispose();
        mouseTrailForm = new MouseTrailForm();
        mouseTrailForm.PresentationFailed += OnMouseTrailPresentationFailed;
        if (loadedConfig is not null)
        {
            mouseTrailForm.ApplySettings(loadedConfig.Config.UiSettings.MouseTrail);
            mouseTrailForm.ApplyHintSettings(loadedConfig.Config.UiSettings.GestureHint);
            mouseTrailForm.ApplyLevelOsdSettings(loadedConfig.Config.UiSettings.LevelOsd);
        }

        return mouseTrailForm;
    }

    private void ApplyUiSettings(GestureUiSettings uiSettings)
    {
        ApplyWindowTheme(uiSettings.Appearance);
        gestureService?.ApplyGestureSensitivity(uiSettings.GestureSensitivity);
        var hasEnabledOverlay = HasEnabledOverlay(uiSettings);
        if (mouseTrailForm is not null || hasEnabledOverlay)
        {
            var trailForm = EnsureMouseTrailForm();
            trailForm.ApplySettings(uiSettings.MouseTrail);
            trailForm.ApplyHintSettings(uiSettings.GestureHint);
            trailForm.ApplyLevelOsdSettings(uiSettings.LevelOsd);
            if (hasEnabledOverlay)
            {
                trailForm.Preload();
            }

            if (!hasEnabledOverlay)
            {
                trailForm.HideTrail();
            }
        }
    }

    private void ShowLevelOsd(LevelOsdRequest request)
    {
        if (!CanUseUi())
        {
            return;
        }

        EnsureMouseTrailForm().ShowLevelOsd(request);
    }

    private void DisposeMouseTrailForm()
    {
        if (mouseTrailForm is null)
        {
            return;
        }

        if (!mouseTrailForm.IsDisposed)
        {
            mouseTrailForm.PresentationFailed -= OnMouseTrailPresentationFailed;
            mouseTrailForm.Close();
            mouseTrailForm.Dispose();
        }

        mouseTrailForm = null;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        ScheduleOverlayRecovery("display-settings-changed");
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            ScheduleOverlayRecovery("power-resume");
        }
    }

    private void OnMouseTrailPresentationFailed(object? sender, EventArgs e)
    {
        ScheduleOverlayRecovery("layered-window-present-failed");
    }

    private void ScheduleOverlayRecovery(string reason)
    {
        if (mouseTrailForm is null || !CanUseUi() ||
            Interlocked.Exchange(ref overlayRecoveryScheduled, 1) != 0)
        {
            return;
        }

        try
        {
            BeginInvoke(() =>
            {
                try
                {
                    if (!CanUseUi() || mouseTrailForm is null)
                    {
                        return;
                    }

                    AppLogger.Information("MainForm", "overlay-recreated", $"Recreating the transparent overlay. Reason: {reason}.");
                    DisposeMouseTrailForm();
                    if (loadedConfig is not null && HasEnabledOverlay(loadedConfig.Config.UiSettings))
                    {
                        EnsureMouseTrailForm().Preload();
                    }
                }
                finally
                {
                    Volatile.Write(ref overlayRecoveryScheduled, 0);
                }
            });
        }
        catch (InvalidOperationException)
        {
            Volatile.Write(ref overlayRecoveryScheduled, 0);
        }
    }

    private static bool HasEnabledOverlay(GestureUiSettings uiSettings)
    {
        return IsFeatureEnabled(uiSettings.MouseTrail.Enabled) ||
               IsFeatureEnabled(uiSettings.GestureHint.Enabled) ||
               IsFeatureEnabled(uiSettings.LevelOsd.Enabled);
    }

    private static bool IsFeatureEnabled(bool? enabled)
    {
        return enabled != false;
    }

}
