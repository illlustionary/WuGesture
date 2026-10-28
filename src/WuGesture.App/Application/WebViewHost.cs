using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace WuGesture.App;

internal sealed class WebViewHost : IDisposable
{
    private const string BrowserProcessName = "msedgewebview2";
    private static readonly TimeSpan BrowserShutdownTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan BrowserShutdownPollInterval = TimeSpan.FromMilliseconds(250);

    private readonly Control owner;
    private readonly Control contentHost;
    private readonly Func<bool> canUseUi;
    private readonly Action<string> onMessageReceived;
    private readonly string userDataFolder;
    private WebView2? webView;
    private CoreWebView2Environment? environment;
    private Color defaultBackgroundColor = SystemColors.Window;
    private string initialTheme = "light";
    private bool isInitializing;
    private TaskCompletionSource? navigationCompletion;

    public WebViewHost(Control owner, Control contentHost, Func<bool> canUseUi, Action<string> onMessageReceived)
    {
        this.owner = owner;
        this.contentHost = contentHost;
        this.canUseUi = canUseUi;
        this.onMessageReceived = onMessageReceived;
        userDataFolder = WebViewHostContract.ResolveUserDataFolder();
    }

    public async Task EnsureAsync()
    {
        if (webView is { IsDisposed: false, CoreWebView2: not null } || isInitializing)
        {
            return;
        }

        isInitializing = true;
        try
        {
            AppLogger.Information("WebViewHost", "initializing", "Initializing the WebView2 configuration host.");
            Dispose();
            Directory.CreateDirectory(userDataFolder);
            environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            if (!canUseUi())
            {
                Dispose();
                return;
            }

            var createdWebView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = defaultBackgroundColor
            };
            webView = createdWebView;
            contentHost.Controls.Add(createdWebView);
            createdWebView.BringToFront();

            await createdWebView.EnsureCoreWebView2Async(environment);
            if (!canUseUi() || createdWebView.IsDisposed || !ReferenceEquals(webView, createdWebView))
            {
                return;
            }

            var coreWebView = createdWebView.CoreWebView2;
            coreWebView.WebMessageReceived += OnWebMessageReceived;
            coreWebView.ProcessFailed += OnProcessFailed;
            ApplyHostSettings(coreWebView.Settings);
            ConfigureHostMapping(coreWebView);
            AppLogger.Information(
                "WebViewHost",
                "ready",
                $"Serving the configuration UI with browser process {coreWebView.BrowserProcessId} from '{userDataFolder}'.");

            await coreWebView.AddScriptToExecuteOnDocumentCreatedAsync($$"""
                (() => {
                    const applyTheme = () => {
                        if (document.documentElement) {
                            document.documentElement.dataset.theme = '{{initialTheme}}';
                        }
                    };

                    applyTheme();
                    document.addEventListener('DOMContentLoaded', applyTheme, { once: true });
                })();
                """);
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            navigationCompletion = completion;
            void OnNavigationCompleted(object? _, CoreWebView2NavigationCompletedEventArgs __) => completion.TrySetResult();

            coreWebView.NavigationCompleted += OnNavigationCompleted;
            try
            {
                createdWebView.Source = WebViewHostContract.EntryUri;
                await completion.Task;
                AppLogger.Information("WebViewHost", "navigation-completed", "The WebView2 configuration host completed its initial navigation.");
            }
            finally
            {
                if (createdWebView.CoreWebView2 is not null)
                {
                    createdWebView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                }

                if (ReferenceEquals(navigationCompletion, completion))
                {
                    navigationCompletion = null;
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException) when (!canUseUi() || webView is null)
        {
        }
        catch (Exception exception)
        {
            AppLogger.Error("WebViewHost", "initialization-failed", "Unable to initialize the WebView2 configuration host.", exception);
            Dispose();
        }
        finally
        {
            isInitializing = false;
        }
    }

    public bool IsReady => webView is { IsDisposed: false, CoreWebView2: not null };

    public void TryPostJson(string payload)
    {
        if (webView is not { IsDisposed: false, CoreWebView2: not null })
        {
            return;
        }

        try
        {
            webView.CoreWebView2.PostWebMessageAsJson(payload);
        }
        catch (Exception exception)
        {
            AppLogger.Warning("WebViewHost", "post-message-failed", "Unable to post a message to the WebView2 host.", exception);
        }
    }

    public void SetInitialAppearance(Color backgroundColor, bool useDarkTheme)
    {
        defaultBackgroundColor = backgroundColor;
        initialTheme = useDarkTheme ? "dark" : "light";
        if (webView is { IsDisposed: false, CoreWebView2: null })
        {
            webView.DefaultBackgroundColor = backgroundColor;
        }
    }

    public void Dispose()
    {
        navigationCompletion?.TrySetCanceled();
        navigationCompletion = null;
        if (webView is null && environment is null)
        {
            return;
        }

        var browserProcessId = TryGetBrowserProcessId(webView);
        if (webView is { IsDisposed: false })
        {
            if (webView.CoreWebView2 is { } coreWebView)
            {
                coreWebView.WebMessageReceived -= OnWebMessageReceived;
                coreWebView.ProcessFailed -= OnProcessFailed;
            }

            contentHost.Controls.Remove(webView);
            webView.Dispose();
        }

        webView = null;
        environment = null;
        AppLogger.Information(
            "WebViewHost",
            "disposed",
            $"Released the WebView2 configuration host. Browser process: {browserProcessId?.ToString(CultureInfo.InvariantCulture) ?? "unknown"}.");
        if (browserProcessId is int processId)
        {
            _ = ReapBrowserProcessAsync(processId);
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        onMessageReceived(args.WebMessageAsJson);
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs args)
    {
        AppLogger.Warning(
            "WebViewHost",
            "process-failed",
            $"The WebView2 host reported a failed process: {args.ProcessFailedKind}.");
    }

    private static void ApplyHostSettings(CoreWebView2Settings settings)
    {
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
        settings.IsSwipeNavigationEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
#if !DEBUG
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
#endif
    }

    private static void ConfigureHostMapping(CoreWebView2 coreWebView)
    {
        var webOutputPath = Path.Combine(
            AppContext.BaseDirectory,
            WebViewHostContract.OutputRootFolder);
        if (!Directory.Exists(webOutputPath))
        {
            throw new DirectoryNotFoundException(
                $"Web frontend output was not found at '{webOutputPath}'. Run 'dotnet build WuGesture.slnx' from the repository root first.");
        }

        coreWebView.SetVirtualHostNameToFolderMapping(
            WebViewHostContract.HostName,
            webOutputPath,
            CoreWebView2HostResourceAccessKind.Allow);
    }

    private static int? TryGetBrowserProcessId(WebView2? view)
    {
        try
        {
            if (view is not { IsDisposed: false, CoreWebView2: { } coreWebView })
            {
                return null;
            }

            return unchecked((int)coreWebView.BrowserProcessId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// WebView2 can keep its browser process alive after the last controller is closed so that the next
    /// creation is faster (CoreWebView2Environment has no teardown API in this SDK). That keeps the whole
    /// configuration UI resident while the window sits in the tray, so the process is reclaimed once it is
    /// clearly idle and no newer WebView is using it.
    /// </summary>
    private async Task ReapBrowserProcessAsync(int browserProcessId)
    {
        try
        {
            var deadline = DateTime.UtcNow + BrowserShutdownTimeout;
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(BrowserShutdownPollInterval);
                if (!IsProcessAlive(browserProcessId))
                {
                    AppLogger.Information(
                        "WebViewHost",
                        "browser-process-exited",
                        $"WebView2 browser process {browserProcessId} exited after the configuration UI was released.");
                    return;
                }
            }

            if (IsWebViewActive())
            {
                return;
            }

            using var process = Process.GetProcessById(browserProcessId);
            if (!string.Equals(process.ProcessName, BrowserProcessName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            process.Kill(entireProcessTree: true);
            AppLogger.Warning(
                "WebViewHost",
                "browser-process-reclaimed",
                $"WebView2 browser process {browserProcessId} was still running {BrowserShutdownTimeout.TotalSeconds:0} s after release and was terminated to reclaim its memory.");
        }
        catch (Exception exception)
        {
            AppLogger.Warning(
                "WebViewHost",
                "browser-process-reclaim-failed",
                "Unable to confirm or reclaim the WebView2 browser process.",
                exception);
        }
    }

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when a WebView2 control is present again (or being created), so a pending reclaim must
    /// not touch the browser process the new control may already be reusing.
    /// </summary>
    private bool IsWebViewActive()
    {
        try
        {
            if (owner.IsDisposed || owner.Disposing || !owner.IsHandleCreated)
            {
                return true;
            }

            return owner.Invoke(new Func<bool>(() => isInitializing || webView is { IsDisposed: false }));
        }
        catch (Exception)
        {
            return true;
        }
    }
}
