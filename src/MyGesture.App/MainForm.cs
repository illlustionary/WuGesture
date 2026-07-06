using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using MyGesture.App.GestureEngine;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Security.Principal;
using System.Text.Json;

namespace MyGesture.App;

public sealed class MainForm : Form
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRegistryValueName = "MyGesture";
    private readonly GestureHintForm gestureHintForm = new();
    private readonly GestureConfigStore configStore = new();
    private readonly KeyboardShortcutRecorder hotkeyRecorder = new();
    private readonly string windowStatePath = GetWindowStatePath();
    private readonly NotifyIcon trayIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
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

    private static readonly JsonSerializerOptions WebMessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WindowStateJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public MainForm()
    {
        Text = "My Gesture";
        StartPosition = FormStartPosition.Manual;
        ApplyInitialWindowState();
        InitializeTrayIcon();

        Load += OnLoad;
        FormClosing += OnFormClosing;
        FormClosed += (_, _) =>
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
            DisposeWebView();
            gestureHintForm.Dispose();
            DisposeMouseTrailForm();
        };
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        if (startMaximized)
        {
            WindowState = FormWindowState.Maximized;
        }

        loadedConfig = configStore.LoadOrCreate();
        ApplyAppBehaviorSettings(loadedConfig.Config.UiSettings.AppBehavior);
        if (TryRelaunchAsAdministrator(loadedConfig.Config.UiSettings.AppBehavior))
        {
            return;
        }

        scopeContextProvider = new ConfiguredScopeContextProvider(loadedConfig.Config.Applications);
        gestureService = new GestureService(new GestureMatcher(loadedConfig.Rules), scopeContextProvider);
        edgeActionService = new EdgeActionService(loadedConfig.Config.EdgeActions);
        ApplyUiSettings(loadedConfig.Config.UiSettings);

        await EnsureWebViewAsync();
        if (!CanUseUi())
        {
            return;
        }

        gestureHintForm.Preload();
        EnsureMouseTrailForm().Preload();

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
    }

    private void InitializeTrayIcon()
    {
        var openItem = new ToolStripMenuItem("打开配置", null, (_, _) => RestoreFromTray());
        var exitItem = new ToolStripMenuItem("退出", null, (_, _) => ExitFromTray());
        trayMenu.Items.Add(openItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(exitItem);

        trayIcon.Text = "My Gesture";
        trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!isExiting && e.CloseReason == CloseReason.UserClosing)
        {
            var closeButtonBehavior = GetCloseButtonBehavior();
            if (closeButtonBehavior == "exit")
            {
                isExiting = true;
            }
            else
            {
                e.Cancel = true;
                if (closeButtonBehavior == "minimize-to-taskbar")
                {
                    MinimizeToTaskbar();
                }
                else
                {
                    MinimizeToTray();
                }

                return;
            }
        }

        isClosing = true;
        SaveWindowState();
        hotkeyRecorder.Stop();
        gestureService?.Dispose();
        edgeActionService?.Dispose();
        hotkeyRecorder.Dispose();
        gestureHintForm.Hide();
        DisposeMouseTrailForm();
        DisposeWebView();
    }

    private void MinimizeToTray()
    {
        SaveWindowState();
        hotkeyRecorder.Stop();
        gestureService?.StopRecording();
        gestureService?.SetPaused(false);
        edgeActionService?.SetPaused(false);
        gestureHintForm.HideResult();
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
        gestureService?.SetPaused(false);
        edgeActionService?.SetPaused(false);
        gestureHintForm.HideResult();
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
        Show();
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
        await EnsureWebViewAsync();
    }

    private void ExitFromTray()
    {
        isExiting = true;
        Close();
    }

    private string GetCloseButtonBehavior()
    {
        return loadedConfig?.Config.UiSettings.AppBehavior.CloseButtonBehavior is
            "minimize-to-tray" or "minimize-to-taskbar" or "exit"
            ? loadedConfig.Config.UiSettings.AppBehavior.CloseButtonBehavior
            : "minimize-to-tray";
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
                key.SetValue(StartupRegistryValueName, $"\"{Application.ExecutablePath}\"");
            }
            else
            {
                key.DeleteValue(StartupRegistryValueName, throwOnMissingValue: false);
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
                FileName = Application.ExecutablePath,
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
            createdWebView.Source = new Uri("https://appassets.local/index.html");
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

        var webDistPath = Path.Combine(AppContext.BaseDirectory, "Web", "dist");
        if (!Directory.Exists(webDistPath))
        {
            throw new DirectoryNotFoundException(
                $"Web frontend output was not found at '{webDistPath}'. Run 'dotnet build MyGesture.slnx' from the repository root first.");
        }

        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets.local",
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

        mouseTrailForm?.SetHighlighted(true);
        gestureHintForm.ShowResult(e.ActionName, autoHide: false);
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

        mouseTrailForm?.SetHighlighted(false);
        gestureHintForm.ClearResult();
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
            type = "gesture",
            pattern = e.Pattern.Select(x => x.ToString()).ToArray(),
            action = e.ActionName
        });

        TryPostWebMessage(payload);
        gestureHintForm.ShowResult(e.ActionName, autoHide: true);
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
            type = "gesture-recorded",
            requestId = e.RequestId,
            button = e.Button.ToString().ToLowerInvariant(),
            pattern = e.Pattern.Select(x => x.ToString()).ToArray()
        });

        gestureHintForm.HideResult();
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
            type = "gesture-action-failed",
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
            type = "edge-action-failed",
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

        if (!e.IsTracking || e.Path.Count < 2)
        {
            mouseTrailForm?.HideTrail();
            return;
        }

        EnsureMouseTrailForm().ShowPath(e.Path, e.Button);
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
            type = "hotkey-recorded",
            requestId = e.RequestId,
            keys = e.Keys
        });

        TryPostWebMessage(payload);
    }

    private void PostStatus(string status)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "status",
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
            type = "rules",
            configPath = loadedConfig.FilePath,
            uiSettings = loadedConfig.Config.UiSettings,
            rules = loadedConfig.Config.Rules.Select(rule => new
            {
                scope = rule.Scope,
                mouseButton = string.IsNullOrWhiteSpace(rule.MouseButton) ? "right" : rule.MouseButton,
                pattern = rule.Pattern,
                actionName = rule.ActionName,
                actionType = string.IsNullOrWhiteSpace(rule.Action.Type) ? "hotkey" : rule.Action.Type,
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
                actionName = action.ActionName,
                action = new
                {
                    type = string.IsNullOrWhiteSpace(action.Action.Type) ? "hotkey" : action.Action.Type,
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
                category = application.Category,
                icon = GetApplicationIconDataUrl(application.Path)
            }).ToArray()
        });

        TryPostWebMessage(payload);
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
            root.GetString() == "get-status")
        {
            PostStatus("running");
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
            case "select-application":
                SelectApplication(json);
                break;
            case "pick-application-window":
                PickApplicationWindow(json);
                break;
            case "start-gesture-recording":
                StartGestureRecording(json);
                break;
            case "stop-gesture-recording":
                StopGestureRecording();
                break;
            case "set-gesture-paused":
                SetGesturePaused(json);
                break;
            case "start-hotkey-recording":
                StartHotkeyRecording(json);
                break;
            case "stop-hotkey-recording":
                hotkeyRecorder.Stop();
                break;
            case "save-rules":
                SaveRules(json);
                break;
            case "reload-rules":
                ReloadRules();
                break;
            case "reset-rules":
                ResetRules();
                break;
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
            gestureService?.SetPaused(message?.Paused ?? false);
            edgeActionService?.SetPaused(message?.Paused ?? false);
            if (message?.Paused == true)
            {
                gestureHintForm.HideResult();
                mouseTrailForm?.HideTrail();
            }
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
            gestureHintForm.HideResult();
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void StopGestureRecording()
    {
        gestureService?.StopRecording();
        gestureHintForm.HideResult();
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
            type = "application-selected",
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
            scopeContextProvider?.UpdateApplications(loadedConfig.Config.Applications);
            gestureService?.UpdateMatcher(new GestureMatcher(loadedConfig.Rules));
            edgeActionService?.UpdateActions(loadedConfig.Config.EdgeActions);
            ApplyAppBehaviorSettings(loadedConfig.Config.UiSettings.AppBehavior);
            ApplyUiSettings(loadedConfig.Config.UiSettings);

            PostRules();
            PostConfigResult(true, "已保存");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void ReloadRules()
    {
        try
        {
            loadedConfig = configStore.LoadOrCreate();
            scopeContextProvider?.UpdateApplications(loadedConfig.Config.Applications);
            gestureService?.UpdateMatcher(new GestureMatcher(loadedConfig.Rules));
            edgeActionService?.UpdateActions(loadedConfig.Config.EdgeActions);
            ApplyAppBehaviorSettings(loadedConfig.Config.UiSettings.AppBehavior);
            ApplyUiSettings(loadedConfig.Config.UiSettings);

            PostRules();
            PostConfigResult(true, "已重新加载");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void ResetRules()
    {
        try
        {
            loadedConfig = configStore.ResetToDefaults();
            scopeContextProvider?.UpdateApplications(loadedConfig.Config.Applications);
            gestureService?.UpdateMatcher(new GestureMatcher(loadedConfig.Rules));
            edgeActionService?.UpdateActions(loadedConfig.Config.EdgeActions);
            ApplyAppBehaviorSettings(loadedConfig.Config.UiSettings.AppBehavior);
            ApplyUiSettings(loadedConfig.Config.UiSettings);

            PostRules();
            PostConfigResult(true, "已恢复默认");
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void PostConfigResult(bool success, string message)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "config-result",
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
            var bounds = WindowState == FormWindowState.Maximized ? RestoreBounds : Bounds;
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
            if (loadedWindowState is null || loadedWindowState.Width <= 0 || loadedWindowState.Height <= 0)
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
        var width = Math.Min(Math.Max(1, bounds.Width), workingArea.Width);
        var height = Math.Min(Math.Max(1, bounds.Height), workingArea.Height);

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
        var width = Math.Max(1, workingArea.Width / 2);
        var height = Math.Max(1, workingArea.Height / 2);
        var left = workingArea.Left + Math.Max(0, (workingArea.Width - width) / 2);
        var top = workingArea.Top + Math.Max(0, (workingArea.Height - height) / 2);
        return new Rectangle(left, top, width, height);
    }

    private static string GetWindowStatePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "MyGesture", "window-state.json");
    }

    private MouseTrailForm EnsureMouseTrailForm()
    {
        if (mouseTrailForm is { IsDisposed: false })
        {
            return mouseTrailForm;
        }

        mouseTrailForm?.Dispose();
        mouseTrailForm = new MouseTrailForm();
        return mouseTrailForm;
    }

    private void ApplyUiSettings(GestureUiSettings uiSettings)
    {
        gestureHintForm.ApplySettings(uiSettings.GestureHint);
        EnsureMouseTrailForm().ApplySettings(uiSettings.MouseTrail);
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
