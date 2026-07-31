using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing;

namespace WuGesture.App;

internal sealed class WebViewHost : IDisposable
{
    private readonly Control owner;
    private readonly Func<bool> canUseUi;
    private readonly Action<string> onMessageReceived;
    private WebView2? webView;
    private Color defaultBackgroundColor = SystemColors.Window;
    private string initialTheme = "light";
    private bool isInitializing;
    private TaskCompletionSource? navigationCompletion;

    public WebViewHost(Control owner, Func<bool> canUseUi, Action<string> onMessageReceived)
    {
        this.owner = owner;
        this.canUseUi = canUseUi;
        this.onMessageReceived = onMessageReceived;
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
            Dispose();
            var createdWebView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = defaultBackgroundColor
            };
            webView = createdWebView;
            owner.Controls.Add(createdWebView);
            createdWebView.BringToFront();

            await createdWebView.EnsureCoreWebView2Async();
            if (!canUseUi() || createdWebView.IsDisposed || !ReferenceEquals(webView, createdWebView))
            {
                return;
            }

            createdWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            ConfigureHostMapping(createdWebView.CoreWebView2);
            await createdWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($$"""
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

            createdWebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            try
            {
                createdWebView.Source = WebViewHostContract.EntryUri;
                await completion.Task;
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
        finally
        {
            isInitializing = false;
        }
    }

    public void TryPostJson(string payload)
    {
        if (webView is not { IsDisposed: false, CoreWebView2: not null })
        {
            return;
        }

        webView.CoreWebView2.PostWebMessageAsJson(payload);
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

            owner.Controls.Remove(webView);
            webView.Dispose();
        }

        webView = null;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        onMessageReceived(args.WebMessageAsJson);
    }

    private static void ConfigureHostMapping(CoreWebView2 coreWebView)
    {
        var webDistPath = Path.Combine(
            AppContext.BaseDirectory,
            WebViewHostContract.OutputRootFolder,
            WebViewHostContract.OutputDistFolder);
        if (!Directory.Exists(webDistPath))
        {
            throw new DirectoryNotFoundException(
                $"Web frontend output was not found at '{webDistPath}'. Run 'dotnet build WuGesture.slnx' from the repository root first.");
        }

        coreWebView.SetVirtualHostNameToFolderMapping(
            WebViewHostContract.HostName,
            webDistPath,
            CoreWebView2HostResourceAccessKind.Allow);
    }
}
