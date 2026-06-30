using Microsoft.Web.WebView2.WinForms;
using MyGesture.App.GestureEngine;
using System.Text.Json;

namespace MyGesture.App;

public sealed class MainForm : Form
{
    private readonly WebView2 webView = new();
    private readonly GestureHintForm gestureHintForm = new();
    private readonly GestureConfigStore configStore = new();
    private ConfiguredScopeContextProvider? scopeContextProvider;
    private LoadedGestureConfig? loadedConfig;
    private GestureService? gestureService;
    private bool isClosing;

    private static readonly JsonSerializerOptions WebMessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MainForm()
    {
        Text = "My Gesture";
        Width = 1080;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        webView.Dock = DockStyle.Fill;
        Controls.Add(webView);

        Load += OnLoad;
        FormClosing += (_, _) =>
        {
            isClosing = true;
            gestureService?.Dispose();
            gestureHintForm.Hide();
        };
        FormClosed += (_, _) =>
        {
            gestureHintForm.Dispose();
        };
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        loadedConfig = configStore.LoadOrCreate();
        scopeContextProvider = new ConfiguredScopeContextProvider(loadedConfig.Config.Applications);
        gestureService = new GestureService(new GestureMatcher(loadedConfig.Rules), scopeContextProvider);

        await webView.EnsureCoreWebView2Async();
        if (!CanUseUi())
        {
            return;
        }

        webView.CoreWebView2.WebMessageReceived += (_, args) => HandleWebMessage(args.WebMessageAsJson);
        ConfigureWebViewHostMapping();

        webView.Source = new Uri("https://appassets.local/index.html");

        gestureService.GesturePreviewMatched += OnGesturePreviewMatched;
        gestureService.GesturePreviewCleared += OnGesturePreviewCleared;
        gestureService.GestureRecognized += OnGestureRecognized;
        gestureService.GestureActionFailed += OnGestureActionFailed;
        gestureService.Start();
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

        webView.CoreWebView2?.PostWebMessageAsJson(payload);
        gestureHintForm.ShowResult(e.ActionName, autoHide: true);
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

        webView.CoreWebView2?.PostWebMessageAsJson(payload);
    }

    private void PostStatus(string status)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "status",
            status
        });

        webView.CoreWebView2?.PostWebMessageAsJson(payload);
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
            rules = loadedConfig.Config.Rules.Select(rule => new
            {
                scope = rule.Scope,
                pattern = rule.Pattern,
                actionName = rule.ActionName,
                actionType = rule.Action.Type,
                keys = rule.Action.Keys
            }).ToArray(),
            applications = loadedConfig.Config.Applications.Select(application => new
            {
                name = application.Name,
                path = application.Path,
                category = application.Category
            }).ToArray()
        });

        webView.CoreWebView2?.PostWebMessageAsJson(payload);
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

            var payload = JsonSerializer.Serialize(new
            {
                type = "application-selected",
                requestId = message?.RequestId ?? "",
                name = Path.GetFileNameWithoutExtension(dialog.FileName),
                path = dialog.FileName,
                category = message?.Category ?? ""
            });

            webView.CoreWebView2?.PostWebMessageAsJson(payload);
        }
        catch (Exception exception)
        {
            PostConfigResult(false, exception.Message);
        }
    }

    private void SaveRules(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<RulesWebMessage>(json, WebMessageJsonOptions);
            var config = new GestureConfig
            {
                Rules = message?.Rules ?? [],
                Applications = message?.Applications ?? []
            };

            loadedConfig = configStore.SaveAndLoad(config);
            scopeContextProvider?.UpdateApplications(loadedConfig.Config.Applications);
            gestureService?.UpdateMatcher(new GestureMatcher(loadedConfig.Rules));

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

        webView.CoreWebView2?.PostWebMessageAsJson(payload);
    }

    private void ConfigureWebViewHostMapping()
    {
        var webDistPath = Path.Combine(AppContext.BaseDirectory, "Web", "dist");
        if (!Directory.Exists(webDistPath))
        {
            throw new DirectoryNotFoundException(
                $"Web frontend output was not found at '{webDistPath}'. Run 'dotnet build MyGesture.slnx' from the repository root first.");
        }

        webView.CoreWebView2!.SetVirtualHostNameToFolderMapping(
            "appassets.local",
            webDistPath,
            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
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

    private sealed class RulesWebMessage
    {
        public string Type { get; set; } = "";

        public List<GestureRuleConfig> Rules { get; set; } = [];

        public List<GestureApplicationConfig> Applications { get; set; } = [];
    }

    private sealed class SelectApplicationWebMessage
    {
        public string Type { get; set; } = "";

        public string RequestId { get; set; } = "";

        public string Category { get; set; } = "";
    }
}
