using Microsoft.Web.WebView2.WinForms;
using MyGesture.App.GestureEngine;
using System.Text.Json;

namespace MyGesture.App;

public sealed class MainForm : Form
{
    private readonly WebView2 webView = new();
    private readonly GestureService gestureService = new();
    private readonly GestureHintForm gestureHintForm = new();
    private bool isClosing;

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
            gestureService.Dispose();
            gestureHintForm.Hide();
        };
        FormClosed += (_, _) =>
        {
            gestureHintForm.Dispose();
        };
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        await webView.EnsureCoreWebView2Async();
        if (!CanUseUi())
        {
            return;
        }

        webView.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            if (args.TryGetWebMessageAsString() == "get-status")
            {
                PostStatus("running");
            }
        };

        var webRoot = Path.Combine(AppContext.BaseDirectory, "Web", "index.html");
        webView.Source = new Uri(webRoot);

        gestureService.GestureRecognized += OnGestureRecognized;
        gestureService.GestureProgressChanged += OnGestureProgressChanged;
        gestureService.Start();
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
        gestureHintForm.Complete(e.Pattern, e.ActionName);
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

        if (e.IsTracking)
        {
            gestureHintForm.ShowProgress(e.Pattern);
        }
        else
        {
            gestureHintForm.Complete(e.Pattern);
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = "gesture-progress",
            pattern = e.Pattern.Select(x => x.ToString()).ToArray(),
            tracking = e.IsTracking
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
}
