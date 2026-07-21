using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using WuGesture.App.GestureEngine;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;

namespace WuGesture.App;

public sealed class MainForm : Form
{
    private const int SwShow = 5;
    private const int SwRestore = 9;
    private const int WmClose = 0x0010;
    private const int DefaultWindowWidth = 1080;
    private const int DefaultWindowHeight = 720;
    private const int MinimumWindowWidth = 640;
    private const int MinimumWindowHeight = 480;
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly GestureConfigStore configStore = new();
    private readonly WebDavConfigSyncService webDavConfigSyncService = new();
    private readonly KeyboardShortcutRecorder hotkeyRecorder = new();
    private readonly string windowStatePath = GetWindowStatePath();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly bool startHiddenToTray;
    private ToolStripMenuItem? pauseItem;
    private Icon? normalTrayIcon;
    private Icon? pausedTrayIcon;
    private WebView2? webView;
    private ConfiguredScopeContextProvider? scopeContextProvider;
    private LoadedGestureConfig? loadedConfig;
    private GestureService? gestureService;
    private EdgeActionService? edgeActionService;
    private MouseTrailForm? mouseTrailForm;
    private bool startMaximized;
    private bool isClosing;
    private bool isExiting;
    private bool isWebViewInitializing;
    private bool isUserPaused;
    private bool isConfigPaused;
    private bool isEditorPaused;

    private static readonly JsonSerializerOptions WebMessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WindowStateJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public MainForm(bool startHiddenToTray = false)
    {
        this.startHiddenToTray = startHiddenToTray;
        Text = AppIdentity.DisplayName;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        StartPosition = FormStartPosition.Manual;
        ApplyInitialWindowState();
        if (startHiddenToTray)
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
        }

        InitializeTrayIcon();

        Load += OnLoad;
        FormClosing += OnFormClosing;
        FormClosed += (_, _) =>
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
            normalTrayIcon?.Dispose();
            pausedTrayIcon?.Dispose();
            DisposeWebView();
            LevelOsdOverlay.Reset();
            DisposeMouseTrailForm();
        };
    }

    protected override void WndProc(ref Message m)
    {
        // Gesture window actions use WM_CLOSE directly, before WinForms assigns a CloseReason.
        if (m.Msg == WmClose && HandleConfiguredUserClose())
        {
            return;
        }

        base.WndProc(ref m);
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        if (startMaximized && !startHiddenToTray)
        {
            WindowState = FormWindowState.Maximized;
        }

        loadedConfig = configStore.LoadOrCreate();
        LevelOsdOverlay.Configure(
            SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext(),
            ShowLevelOsd);
        isConfigPaused = loadedConfig.Config.UiSettings.AppBehavior.GesturePaused;
        ApplyAppBehaviorSettings(loadedConfig.Config.UiSettings.AppBehavior);
        if (TryRelaunchAsAdministrator(loadedConfig.Config.UiSettings.AppBehavior))
        {
            return;
        }

        scopeContextProvider = new ConfiguredScopeContextProvider(loadedConfig.Config.Applications);
        gestureService = new GestureService(new GestureMatcher(loadedConfig.Rules), scopeContextProvider);
        gestureService.UpdateExcludedApplications(loadedConfig.Config.UiSettings.AppBehavior.ExcludedApplications);
        gestureService.UpdateFullscreenBehavior(loadedConfig.Config.UiSettings.AppBehavior.DisableGesturesInFullscreen);
        edgeActionService = new EdgeActionService(loadedConfig.Config.EdgeActions);
        edgeActionService.UpdateExcludedApplications(loadedConfig.Config.UiSettings.AppBehavior.ExcludedApplications);
        edgeActionService.UpdateFullscreenBehavior(loadedConfig.Config.UiSettings.AppBehavior.DisableEdgeActionsInFullscreen);
        ApplyUiSettings(loadedConfig.Config.UiSettings);

        if (!startHiddenToTray)
        {
            await EnsureWebViewAsync();
        }

        if (!CanUseUi())
        {
            return;
        }

        if (IsFeatureEnabled(loadedConfig.Config.UiSettings.GestureHint.Enabled) ||
            IsFeatureEnabled(loadedConfig.Config.UiSettings.MouseTrail.Enabled) ||
            IsFeatureEnabled(loadedConfig.Config.UiSettings.LevelOsd.Enabled))
        {
            EnsureMouseTrailForm().Preload();
        }

        gestureService.GesturePreviewMatched += OnGesturePreviewMatched;
        gestureService.GesturePreviewCleared += OnGesturePreviewCleared;
        gestureService.GestureRecognized += OnGestureRecognized;
        gestureService.GestureRecordingCompleted += OnGestureRecordingCompleted;
        gestureService.GestureActionFailed += OnGestureActionFailed;
        gestureService.GestureProgressChanged += OnGestureProgressChanged;
        edgeActionService.EdgeActionFailed += OnEdgeActionFailed;
        hotkeyRecorder.HotkeyRecorded += OnHotkeyRecorded;
        gestureService.Start();
        edgeActionService.Start();
        ApplyGesturePauseState();

        if (startHiddenToTray)
        {
            HideStartupWindow();
        }
    }

    private void InitializeTrayIcon()
    {
        var openItem = new ToolStripMenuItem("打开配置", null, (_, _) => RestoreFromTray());
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
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && HandleConfiguredUserClose())
        {
            e.Cancel = true;
            return;
        }

        isClosing = true;
        SaveWindowState();
        hotkeyRecorder.Stop();
        gestureService?.Dispose();
        edgeActionService?.Dispose();
        hotkeyRecorder.Dispose();
        DisposeMouseTrailForm();
        DisposeWebView();
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
        DisposeWebView();
        ShowInTaskbar = false;
        Hide();
    }

    private void MinimizeToTaskbar()
    {
        SaveWindowState();
        hotkeyRecorder.Stop();
        gestureService?.StopRecording();
        isEditorPaused = false;
        ApplyGesturePauseState();
        mouseTrailForm?.HideTrail();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Minimized;
    }

    private async void RestoreFromTray()
    {
        if (isClosing || IsDisposed)
        {
            return;
        }

        ShowInTaskbar = true;
        Opacity = 1;
        Show();
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
        await EnsureWebViewAsync();
    }

    private void HideStartupWindow()
    {
        ShowInTaskbar = false;
        Hide();
        WindowState = FormWindowState.Normal;
        Opacity = 1;
    }

    private void ExitFromTray()
    {
        isExiting = true;
        Close();
    }

    public void ShowExistingInstance()
    {
        RestoreFromTray();
        BringWindowToFront();
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
            Process.Start(new ProcessStartInfo
            {
                FileName = AppIdentity.GetLaunchExecutablePath(),
                Arguments = startHiddenToTray
                    ? $"{AppIdentity.ElevatedRelaunchArgument} {AppIdentity.StartupLaunchArgument}"
                    : AppIdentity.ElevatedRelaunchArgument,
                UseShellExecute = true,
                Verb = "runas"
            });
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
        if (webView is { IsDisposed: false, CoreWebView2: not null } || isWebViewInitializing)
        {
            return;
        }

        isWebViewInitializing = true;
        try
        {
            DisposeWebView();
            var createdWebView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            webView = createdWebView;
            Controls.Add(createdWebView);
            createdWebView.BringToFront();

            await createdWebView.EnsureCoreWebView2Async();
            if (!CanUseUi() || createdWebView.IsDisposed || !ReferenceEquals(webView, createdWebView))
            {
                return;
            }

            createdWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            ConfigureWebViewHostMapping();
            createdWebView.Source = WebViewHostContract.EntryUri;
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException) when (isClosing || webView is null)
        {
        }
        finally
        {
            isWebViewInitializing = false;
        }
    }

    private void DisposeWebView()
    {
        if (webView is null)
        {
            return;
        }

        if (!webView.IsDisposed)
        {
            if (webView.CoreWebView2 is not null)
            {
                webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }

            Controls.Remove(webView);
            webView.Dispose();
        }

        webView = null;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        HandleWebMessage(args.WebMessageAsJson);
    }

    private void TryPostWebMessage(string payload)
    {
        if (!CanPostWebMessage())
        {
            return;
        }

        webView!.CoreWebView2!.PostWebMessageAsJson(payload);
    }

    private bool CanPostWebMessage()
    {
        return !isClosing && webView is { IsDisposed: false, CoreWebView2: not null };
    }

    private void ConfigureWebViewHostMapping()
    {
        if (webView?.CoreWebView2 is null)
        {
            return;
        }

        var webDistPath = Path.Combine(
            AppContext.BaseDirectory,
            WebViewHostContract.OutputRootFolder,
            WebViewHostContract.OutputDistFolder);
        if (!Directory.Exists(webDistPath))
        {
            throw new DirectoryNotFoundException(
                $"Web frontend output was not found at '{webDistPath}'. Run 'dotnet build WuGesture.slnx' from the repository root first.");
        }

        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            WebViewHostContract.HostName,
            webDistPath,
            CoreWebView2HostResourceAccessKind.Allow);
    }

    private void OnGesturePreviewMatched(object? sender, GestureRecognizedEventArgs e)
    {
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnGesturePreviewMatched(sender, e));
            return;
        }

        if (IsFeatureEnabled(loadedConfig?.Config.UiSettings.MouseTrail.Enabled))
        {
            mouseTrailForm?.SetHighlighted(true);
        }

        if (IsFeatureEnabled(loadedConfig?.Config.UiSettings.GestureHint.Enabled))
        {
            EnsureMouseTrailForm().ShowGestureHint(e.ActionName, e.Path[^1], autoHide: false);
        }
    }

    private void OnGesturePreviewCleared(object? sender, EventArgs e)
    {
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnGesturePreviewCleared(sender, e));
            return;
        }

        if (IsFeatureEnabled(loadedConfig?.Config.UiSettings.MouseTrail.Enabled))
        {
            mouseTrailForm?.SetHighlighted(false);
        }

        if (IsFeatureEnabled(loadedConfig?.Config.UiSettings.GestureHint.Enabled))
        {
            mouseTrailForm?.ClearGestureHint();
        }
    }

    private void OnGestureRecognized(object? sender, GestureRecognizedEventArgs e)
    {
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnGestureRecognized(sender, e));
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.Gesture,
            pattern = e.Pattern.Select(x => x.ToString()).ToArray(),
            action = e.ActionName
        });

        TryPostWebMessage(payload);
        if (IsFeatureEnabled(loadedConfig?.Config.UiSettings.GestureHint.Enabled))
        {
            EnsureMouseTrailForm().ShowGestureHint(e.ActionName, e.Path[^1], autoHide: true);
        }
    }

    private void OnGestureRecordingCompleted(object? sender, GestureRecordingCompletedEventArgs e)
    {
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnGestureRecordingCompleted(sender, e));
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.GestureRecorded,
            requestId = e.RequestId,
            button = e.Button.ToString().ToLowerInvariant(),
            pattern = e.Pattern.Select(x => x.ToString()).ToArray()
        });

        mouseTrailForm?.HideTrail();
        TryPostWebMessage(payload);
    }

    private void OnGestureActionFailed(object? sender, GestureActionFailedEventArgs e)
    {
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnGestureActionFailed(sender, e));
            return;
        }

        var message = e.Exception.Message;
        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.GestureActionFailed,
            pattern = e.Pattern.Select(x => x.ToString()).ToArray(),
            action = e.ActionName,
            error = message
        });

        TryPostWebMessage(payload);
    }

    private void OnEdgeActionFailed(object? sender, EdgeActionFailedEventArgs e)
    {
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

    private void OnGestureProgressChanged(object? sender, GestureProgressEventArgs e)
    {
        if (!CanUseUi())
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvokeSafe(() => OnGestureProgressChanged(sender, e));
            return;
        }

        var isTrailEnabled = IsFeatureEnabled(loadedConfig?.Config.UiSettings.MouseTrail.Enabled);
        var isHintEnabled = IsFeatureEnabled(loadedConfig?.Config.UiSettings.GestureHint.Enabled);
        if (!isTrailEnabled && !isHintEnabled)
        {
            mouseTrailForm?.HideTrail();
            return;
        }

        if (!e.IsTracking || e.Path.Count < 2)
        {
            mouseTrailForm?.EndPath();
            return;
        }

        if (isTrailEnabled)
        {
            EnsureMouseTrailForm().ShowPath(e.Path, e.Button);
        }
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

        var payload = JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.Rules,
            configPath = loadedConfig.FilePath,
            uiSettings = CreateUiSettingsPayload(loadedConfig.Config.UiSettings),
            rules = loadedConfig.Config.Rules.Select(rule => new
            {
                scope = rule.Scope,
                mouseButton = string.IsNullOrWhiteSpace(rule.MouseButton) ? GestureConfigContract.MouseButtons.Right : rule.MouseButton,
                pattern = rule.Pattern,
                actionName = rule.ActionName,
                actionType = string.IsNullOrWhiteSpace(rule.Action.Type) ? GestureConfigContract.ActionTypes.Hotkey : rule.Action.Type,
                keys = rule.Action.Keys,
                operation = rule.Action.Operation,
                amount = rule.Action.Amount
            }).ToArray(),
            edgeActions = loadedConfig.Config.EdgeActions.Select(action => new
            {
                enabled = action.Enabled,
                triggerType = action.TriggerType,
                location = action.Location,
                wheelDirection = action.WheelDirection,
                frictionCount = action.FrictionCount,
                action = new
                {
                    type = string.IsNullOrWhiteSpace(action.Action.Type) ? GestureConfigContract.ActionTypes.Hotkey : action.Action.Type,
                    keys = action.Action.Keys,
                    operation = action.Action.Operation,
                    amount = action.Action.Amount
                }
            }).ToArray(),
            applications = loadedConfig.Config.Applications.Select(application => new
            {
                name = application.Name,
                displayName = application.DisplayName,
                path = application.Path,
                categories = application.Categories,
                icon = GetApplicationIconDataUrl(application.Path)
            }).ToArray()
        });

        TryPostWebMessage(payload);
    }

    private static object CreateUiSettingsPayload(GestureUiSettings uiSettings)
    {
        return new
        {
            mouseTrail = uiSettings.MouseTrail,
            gestureHint = uiSettings.GestureHint,
            levelOsd = uiSettings.LevelOsd,
            gestureSensitivity = uiSettings.GestureSensitivity,
            appBehavior = new
            {
                launchAtStartup = uiSettings.AppBehavior.LaunchAtStartup,
                runAsAdministrator = uiSettings.AppBehavior.RunAsAdministrator,
                closeButtonBehavior = uiSettings.AppBehavior.CloseButtonBehavior,
                gesturePaused = uiSettings.AppBehavior.GesturePaused,
                disableGesturesInFullscreen = uiSettings.AppBehavior.DisableGesturesInFullscreen,
                disableEdgeActionsInFullscreen = uiSettings.AppBehavior.DisableEdgeActionsInFullscreen,
                excludedApplications = uiSettings.AppBehavior.ExcludedApplications.Select(application => new
                {
                    name = application.Name,
                    displayName = application.DisplayName,
                    path = application.Path,
                    disableEdgeActions = application.DisableEdgeActions,
                    icon = GetApplicationIconDataUrl(application.Path)
                }).ToArray()
            },
            webDav = uiSettings.WebDav
        };
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
            using var picker = new ApplicationTargetPickerForm();
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
            icon = GetApplicationIconDataUrl(path)
        });

        TryPostWebMessage(payload);
    }

    private static string GetApplicationIconDataUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "";
        }

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null)
            {
                return "";
            }

            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
        }
        catch
        {
            return "";
        }
    }

    private void SaveRules(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var uiSettings = message?.UiSettings ?? loadedConfig?.Config.UiSettings ?? new GestureUiSettings();
            var config = new GestureConfig
            {
                Rules = message?.Rules ?? [],
                Applications = message?.Applications ?? [],
                EdgeActions = message?.EdgeActions ?? [],
                UiSettings = uiSettings
            };

            loadedConfig = configStore.SaveAndLoad(config);
            ApplyLoadedConfig();

            PostRules();
            PostConfigResult(true, "已保存", "save");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message, "save");
        }
    }

    private async void TestWebDavConnection(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var settings = message?.UiSettings?.WebDav ?? loadedConfig?.Config.UiSettings.WebDav ?? new WebDavUiSettings();
            await webDavConfigSyncService.TestConnectionAsync(settings);
            PostWebDavResult("test", true, "WebDAV 连接成功");
        }
        catch (Exception exception)
        {
            PostWebDavResult("test", false, exception.Message);
        }
    }

    private async void SaveConfigToWebDav(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var uiSettings = message?.UiSettings ?? loadedConfig?.Config.UiSettings ?? new GestureUiSettings();
            var config = new GestureConfig
            {
                Rules = message?.Rules ?? [],
                Applications = message?.Applications ?? [],
                EdgeActions = message?.EdgeActions ?? [],
                UiSettings = uiSettings
            };

            loadedConfig = configStore.SaveAndLoad(config);
            ApplyLoadedConfig();
            await webDavConfigSyncService.UploadAsync(loadedConfig.FilePath, loadedConfig.Config.UiSettings.WebDav);

            PostRules();
            PostWebDavResult("save", true, "已保存到 WebDAV");
        }
        catch (Exception exception)
        {
            PostWebDavResult("save", false, exception.Message);
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
            PostWebDavResult("restore", true, "已从 WebDAV 恢复");
        }
        catch (Exception exception)
        {
            PostWebDavResult("restore", false, exception.Message);
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
                Rules = message?.Rules ?? [],
                Applications = message?.Applications ?? [],
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
            PostConfigResult(true, "已导出配置", "export");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message, "export");
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
            PostConfigResult(true, "已导入配置", "import");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message, "import");
        }
    }

    private void ReloadRules()
    {
        try
        {
            loadedConfig = configStore.LoadOrCreate();
            ApplyLoadedConfig();

            PostRules();
            PostConfigResult(true, "已重新加载", "reload");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message, "reload");
        }
    }

    private void ResetRules()
    {
        try
        {
            loadedConfig = configStore.ResetToDefaults();
            ApplyLoadedConfig();

            PostRules();
            PostConfigResult(true, "已恢复默认", "reset");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message, "reset");
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

    private void BringWindowToFront()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        if (IsIconic(Handle))
        {
            ShowWindow(Handle, SwRestore);
        }
        else
        {
            ShowWindow(Handle, SwShow);
        }

        Activate();
        SetForegroundWindow(Handle);
        TopMost = true;
        TopMost = false;
    }

    private void ApplyInitialWindowState()
    {
        if (TryLoadWindowState(out var windowState))
        {
            Bounds = NormalizeBounds(windowState.Bounds);
            startMaximized = windowState.Maximized;
            return;
        }

        Bounds = GetDefaultBounds();
    }

    private void SaveWindowState()
    {
        try
        {
            var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            if (!HasUsableWindowSize(bounds))
            {
                return;
            }

            var windowState = new WindowStateData
            {
                X = bounds.X,
                Y = bounds.Y,
                Width = bounds.Width,
                Height = bounds.Height,
                Maximized = WindowState == FormWindowState.Maximized
            };

            var directory = Path.GetDirectoryName(windowStatePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(windowStatePath, JsonSerializer.Serialize(windowState, WindowStateJsonOptions));
        }
        catch
        {
        }
    }

    private bool TryLoadWindowState(out WindowStateData windowState)
    {
        windowState = new WindowStateData();

        try
        {
            if (!File.Exists(windowStatePath))
            {
                return false;
            }

            var json = File.ReadAllText(windowStatePath);
            var loadedWindowState = JsonSerializer.Deserialize<WindowStateData>(json, WindowStateJsonOptions);
            if (loadedWindowState is null || !HasUsableWindowSize(loadedWindowState.Bounds))
            {
                return false;
            }

            windowState = loadedWindowState;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Rectangle NormalizeBounds(Rectangle bounds)
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        var width = Math.Min(Math.Max(MinimumWindowWidth, bounds.Width), workingArea.Width);
        var height = Math.Min(Math.Max(MinimumWindowHeight, bounds.Height), workingArea.Height);

        var left = bounds.Left;
        var top = bounds.Top;

        if (left < workingArea.Left || left + width > workingArea.Right)
        {
            left = workingArea.Left + Math.Max(0, (workingArea.Width - width) / 2);
        }

        if (top < workingArea.Top || top + height > workingArea.Bottom)
        {
            top = workingArea.Top + Math.Max(0, (workingArea.Height - height) / 2);
        }

        return new Rectangle(left, top, width, height);
    }

    private static Rectangle GetDefaultBounds()
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        var width = Math.Min(DefaultWindowWidth, workingArea.Width);
        var height = Math.Min(DefaultWindowHeight, workingArea.Height);
        var left = workingArea.Left + Math.Max(0, (workingArea.Width - width) / 2);
        var top = workingArea.Top + Math.Max(0, (workingArea.Height - height) / 2);
        return new Rectangle(left, top, width, height);
    }

    private static string GetWindowStatePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppIdentity.AppDataFolderName, ConfigStorageContract.WindowStateFileName);
    }

    private static bool HasUsableWindowSize(Rectangle bounds)
    {
        return bounds.Width >= MinimumWindowWidth && bounds.Height >= MinimumWindowHeight;
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

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private MouseTrailForm EnsureMouseTrailForm()
    {
        if (mouseTrailForm is { IsDisposed: false })
        {
            return mouseTrailForm;
        }

        mouseTrailForm?.Dispose();
        mouseTrailForm = new MouseTrailForm();
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
        gestureService?.ApplyGestureSensitivity(uiSettings.GestureSensitivity);
        var hasEnabledOverlay =
            IsFeatureEnabled(uiSettings.MouseTrail.Enabled) ||
            IsFeatureEnabled(uiSettings.GestureHint.Enabled) ||
            IsFeatureEnabled(uiSettings.LevelOsd.Enabled);
        if (mouseTrailForm is not null || hasEnabledOverlay)
        {
            var trailForm = EnsureMouseTrailForm();
            trailForm.ApplySettings(uiSettings.MouseTrail);
            trailForm.ApplyHintSettings(uiSettings.GestureHint);
            trailForm.ApplyLevelOsdSettings(uiSettings.LevelOsd);
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
            mouseTrailForm.Close();
            mouseTrailForm.Dispose();
        }

        mouseTrailForm = null;
    }

    private static bool IsFeatureEnabled(bool? enabled)
    {
        return enabled != false;
    }

    private sealed class RulesWebMessage
    {
        public string Type { get; set; } = "";

        public List<GestureRuleConfig> Rules { get; set; } = [];

        public List<GestureApplicationConfig> Applications { get; set; } = [];

        public List<EdgeActionConfig> EdgeActions { get; set; } = [];

        public GestureUiSettings UiSettings { get; set; } = new();
    }

    private sealed class SelectApplicationWebMessage
    {
        public string Type { get; set; } = "";

        public string RequestId { get; set; } = "";

        public string Category { get; set; } = "";
    }

    private sealed class SetGesturePausedWebMessage
    {
        public string Type { get; set; } = "";

        public bool Paused { get; set; }
    }

    private sealed class StartHotkeyRecordingWebMessage
    {
        public string Type { get; set; } = "";

        public string RequestId { get; set; } = "";
    }

    private sealed class StartGestureRecordingWebMessage
    {
        public string Type { get; set; } = "";

        public string RequestId { get; set; } = "";
    }

    private sealed class PreviewLevelOsdWebMessage
    {
        public string Type { get; set; } = "";

        public string Kind { get; set; } = "";
    }

    private sealed class WindowStateData
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool Maximized { get; set; }

        public Rectangle Bounds => new(X, Y, Width, Height);
    }
}
