using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WuGesture.App;

internal sealed class WebViewHost : IDisposable
{
    private readonly Control owner;
    private readonly Func<bool> canUseUi;
    private readonly Action<string> onMessageReceived;
    private WebView2? webView;
    private bool isInitializing;

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
                Dock = DockStyle.Fill
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
            createdWebView.Source = WebViewHostContract.EntryUri;
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

    public void Dispose()
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
