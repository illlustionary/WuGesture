using System.ComponentModel;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

public sealed partial class MainForm : Form
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly KeyboardShortcutRecorder hotkeyRecorder = new();
    private readonly WindowStateStore windowStateStore = new();
    private readonly bool startHiddenToTray;
    private readonly bool isUpdateRestart;
    private readonly WebViewHost webViewHost;
    private readonly TrayMenuController trayMenuController;
    private readonly ElevationLauncher elevationLauncher;
    private readonly WebMessageRouter webMessageRouter;
    private readonly ConfigurationCoordinator configurationCoordinator;
    private readonly ApplicationPickerCoordinator applicationPickerCoordinator;
    private readonly UpdateCoordinator updateCoordinator;
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
    private bool hasWebUiLoaded;
    private bool hasPostedUpdateCompleted;
    private int overlayRecoveryScheduled;

    public MainForm(bool startHiddenToTray = false, bool isUpdateRestart = false)
    {
        this.startHiddenToTray = startHiddenToTray;
        this.isUpdateRestart = isUpdateRestart;
        ConfigureTitleBarPainting();
        var contentHost = CreateContentHost();
        ApplyTitleBarMetrics();
        webViewHost = new WebViewHost(this, contentHost, CanUseUi, HandleWebMessage);
        configurationCoordinator = new ConfigurationCoordinator(
            () => loadedConfig,
            config =>
            {
                loadedConfig = config;
                ApplyLoadedConfig();
            },
            PostRules,
            PostConfigResult,
            PostWebDavResult,
            () => this);
        applicationPickerCoordinator = new ApplicationPickerCoordinator(
            () => loadedConfig,
            TryPostWebMessage,
            (success, message, operation) => PostConfigResult(success, message, operation),
            () => this);
        updateCoordinator = new UpdateCoordinator(
            () => loadedConfig?.Config.UiSettings.Update,
            TryPostWebMessage,
            action => BeginInvokeSafe(action),
            () => webViewHost.IsReady && hasWebUiLoaded,
            () => Visible && WindowState != FormWindowState.Minimized,
            RequestClose);
        webMessageRouter = new WebMessageRouter(
            CreateWebMessageHandlers(),
            HandleStatusRequest,
            exception => PostConfigResult(false, $"处理配置请求失败：{exception.Message}"));
        loadedConfig = configurationCoordinator.Load();
        AppLogger.Information("MainForm", "config-loaded", "Loaded the local gesture configuration.");
        // The first launch after an update always opens the settings window on the About page so the user can
        // see that the new version is installed, regardless of the auto-start and window settings.
        hideConfigWindowOnLaunch = !isUpdateRestart && (startHiddenToTray ||
            !loadedConfig.Config.UiSettings.AppBehavior.ShowConfigWindowOnLaunch);
        elevationLauncher = new ElevationLauncher(startHiddenToTray, hideConfigWindowOnLaunch, isUpdateRestart, RequestClose);
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

        trayMenuController = new TrayMenuController(
            RestoreFromTray,
            SetUserPaused,
            ExitFromTray,
            loadedConfig.Config.UiSettings.Appearance);

        Load += OnLoad;
        Resize += OnResize;
        FormClosing += OnFormClosing;
        FormClosed += (_, _) =>
        {
            SystemEvents.UserPreferenceChanged -= OnSystemUserPreferenceChanged;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            trayMenuController.Dispose();
            DisposeTitleBarFonts();
            DisposeWebView();
            updateCoordinator.Dispose();
            LevelOsdOverlay.Reset();
            gestureFeedbackCoordinator?.Dispose();
            DisposeMouseTrailForm();
        };
        SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        AppLogger.Information("MainForm", "initializing", $"Initializing services. Hidden startup: {hideConfigWindowOnLaunch}.");
        var config = loadedConfig ??= configurationCoordinator.Load();
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
        if (elevationLauncher.TryRelaunch(config.Config.UiSettings.AppBehavior))
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
        updateCoordinator.StartAutoCheck();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (CloseRequestPolicy.IsCloseRequest(e.CloseReason) && HandleConfiguredUserClose())
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

    private void OnResize(object? sender, EventArgs e)
    {
        InvalidateTitleBar();
        if (WindowState != FormWindowState.Minimized)
        {
            // An update check that finished while the window was hidden or minimized is cached until the UI is
            // promptable. Flush it as soon as the window is back on screen, otherwise it stays stuck until the
            // next check or WebView reload.
            updateCoordinator.PostPendingPrompt();
            return;
        }

        if (!removeTaskbarButtonAfterMinimize)
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
                AppLogger.Information("MainForm", "hidden-to-tray", "The configuration window was hidden; releasing the WebView2 host.");
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

    private async void RestoreFromTray()
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

            // Run after the tray menu or single-instance callback has returned so it cannot reclaim focus, and
            // flush the cached update prompt only once the window is actually restored: while it is still
            // minimized the coordinator defers the prompt, so flushing before this would drop it.
            BeginInvokeSafe(() =>
            {
                BringWindowToFront();
                updateCoordinator.PostPendingPrompt();
            });
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

    public void ShowExistingInstance()
    {
        if (Visible && WindowState != FormWindowState.Minimized)
        {
            BringWindowToFront(attachToForegroundInputFirst: true);
            return;
        }

        RestoreFromTray();
    }

    private void SetUserPaused(bool paused)
    {
        if (isUserPaused != paused)
        {
            isUserPaused = paused;
            trayMenuController.SetPaused(paused);
            ApplyGesturePauseState();
        }

        PostStatus(isUserPaused ? "paused" : "running");
    }

    /// <summary>
    /// Closes the app from the UI thread: tray exit, elevation hand-off, update restart and the
    /// external <c>--exit</c> request all end up here.
    /// </summary>
    public void RequestClose()
    {
        isExiting = true;
        BeginInvokeSafe(Close);
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

    private async Task EnsureWebViewAsync()
    {
        await webViewHost.EnsureAsync();
    }

    private void DisposeWebView()
    {
        hasWebUiLoaded = false;
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

        webMessageRouter.Route(json);
    }

    private void HandleStatusRequest(string? messageType)
    {
        if (!string.Equals(messageType, WebViewMessageTypes.GetStatus, StringComparison.Ordinal))
        {
            AppLogger.Warning(
                "MainForm",
                "unknown-message",
                $"Ignoring an unknown string WebView message: {messageType ?? "null"}.");
            return;
        }

        PostStatus(isUserPaused ? "paused" : "running");
        PostRules();
        hasWebUiLoaded = true;
        PostUpdateCompleted();
        updateCoordinator.PostPendingPrompt();
    }

    private void PostUpdateCompleted()
    {
        if (!isUpdateRestart || hasPostedUpdateCompleted)
        {
            return;
        }

        hasPostedUpdateCompleted = true;
        var version = AppIdentity.GetDisplayVersion();
        AppLogger.Information(
            "MainForm",
            "update-completed",
            $"Showing the About page after the update restart (current version: {version}).");
        TryPostWebMessage(JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.UpdateCompleted,
            version,
            message = $"已更新到 {version}。"
        }));
    }

    private Dictionary<string, Action<string>> CreateWebMessageHandlers()
    {
        return new Dictionary<string, Action<string>>(StringComparer.Ordinal)
        {
            [WebViewMessageTypes.SelectApplication] = applicationPickerCoordinator.Select,
            [WebViewMessageTypes.PickApplicationWindow] = applicationPickerCoordinator.PickWindow,
            [WebViewMessageTypes.OpenApplicationFolder] = applicationPickerCoordinator.OpenFolder,
            [WebViewMessageTypes.StartGestureRecording] = StartGestureRecording,
            [WebViewMessageTypes.StopGestureRecording] = _ => StopGestureRecording(),
            [WebViewMessageTypes.SetGesturePaused] = SetGesturePaused,
            [WebViewMessageTypes.SetUserPaused] = SetUserPaused,
            [WebViewMessageTypes.StartHotkeyRecording] = StartHotkeyRecording,
            [WebViewMessageTypes.StopHotkeyRecording] = _ => hotkeyRecorder.Stop(),
            [WebViewMessageTypes.SaveRules] = configurationCoordinator.Save,
            [WebViewMessageTypes.WebDavTest] = configurationCoordinator.TestWebDavConnection,
            [WebViewMessageTypes.WebDavSave] = configurationCoordinator.SaveToWebDav,
            [WebViewMessageTypes.WebDavRestore] = configurationCoordinator.RestoreFromWebDav,
            [WebViewMessageTypes.ExportConfig] = configurationCoordinator.Export,
            [WebViewMessageTypes.ImportConfig] = _ => configurationCoordinator.Import(),
            [WebViewMessageTypes.ReloadRules] = _ => configurationCoordinator.Reload(),
            [WebViewMessageTypes.ResetRules] = _ => configurationCoordinator.Reset(),
            [WebViewMessageTypes.PreviewLevelOsd] = PreviewLevelOsd,
            [WebViewMessageTypes.CheckUpdate] = _ => updateCoordinator.CheckForUpdate(manual: true),
            [WebViewMessageTypes.StartUpdate] = _ => updateCoordinator.StartUpdate(),
            [WebViewMessageTypes.CancelUpdate] = _ => updateCoordinator.Cancel(),
            [WebViewMessageTypes.OpenUpdatePage] = _ => updateCoordinator.OpenReleasePage()
        };
    }

    private void PreviewLevelOsd(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<PreviewLevelOsdWebMessage>(json, WebMessageJson.Options);
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
            var message = JsonSerializer.Deserialize<StartHotkeyRecordingWebMessage>(json, WebMessageJson.Options);
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
            var message = JsonSerializer.Deserialize<SetGesturePausedWebMessage>(json, WebMessageJson.Options);
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
            var message = JsonSerializer.Deserialize<SetGesturePausedWebMessage>(json, WebMessageJson.Options);
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
            var message = JsonSerializer.Deserialize<StartGestureRecordingWebMessage>(json, WebMessageJson.Options);
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
            // Right after a resume the cursor sample can be bogus: GetCursorPos fails and WinForms falls
            // back to (0, 0), which is the top-left corner, so ignore edge actions while settling.
            edgeActionService?.SuppressFor(TimeSpan.FromSeconds(2));
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
