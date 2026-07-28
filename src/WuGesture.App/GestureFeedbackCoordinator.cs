using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

internal sealed class GestureFeedbackCoordinator : IDisposable
{
    private readonly Control uiControl;
    private readonly GestureService gestureService;
    private readonly Func<GestureUiSettings?> getUiSettings;
    private readonly Func<MouseTrailForm> ensureMouseTrailForm;
    private readonly Func<MouseTrailForm?> getMouseTrailForm;
    private readonly Action<string> postWebMessage;
    private readonly Func<bool> canUseUi;
    private long latestSessionId;
    private bool disposed;

    public GestureFeedbackCoordinator(
        Control uiControl,
        GestureService gestureService,
        Func<GestureUiSettings?> getUiSettings,
        Func<MouseTrailForm> ensureMouseTrailForm,
        Func<MouseTrailForm?> getMouseTrailForm,
        Action<string> postWebMessage,
        Func<bool> canUseUi)
    {
        this.uiControl = uiControl;
        this.gestureService = gestureService;
        this.getUiSettings = getUiSettings;
        this.ensureMouseTrailForm = ensureMouseTrailForm;
        this.getMouseTrailForm = getMouseTrailForm;
        this.postWebMessage = postWebMessage;
        this.canUseUi = canUseUi;

        gestureService.GesturePreviewMatched += OnPreviewMatched;
        gestureService.GesturePreviewCleared += OnPreviewCleared;
        gestureService.GestureRecognized += OnRecognized;
        gestureService.GestureRecordingCompleted += OnRecordingCompleted;
        gestureService.GestureActionFailed += OnActionFailed;
        gestureService.GestureProgressChanged += OnProgressChanged;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        gestureService.GesturePreviewMatched -= OnPreviewMatched;
        gestureService.GesturePreviewCleared -= OnPreviewCleared;
        gestureService.GestureRecognized -= OnRecognized;
        gestureService.GestureRecordingCompleted -= OnRecordingCompleted;
        gestureService.GestureActionFailed -= OnActionFailed;
        gestureService.GestureProgressChanged -= OnProgressChanged;
    }

    private void OnPreviewMatched(object? sender, GestureRecognizedEventArgs e)
    {
        RunOnUi(() =>
        {
            if (!AcceptSession(e.SessionId))
            {
                return;
            }

            var settings = getUiSettings();
            if (IsEnabled(settings?.MouseTrail.Enabled))
            {
                getMouseTrailForm()?.SetHighlighted(true);
            }

            if (IsEnabled(settings?.GestureHint.Enabled))
            {
                ensureMouseTrailForm().ShowGestureHint(e.ActionName, e.Path[^1], autoHide: false);
            }
        });
    }

    private void OnPreviewCleared(object? sender, GesturePreviewClearedEventArgs e)
    {
        RunOnUi(() =>
        {
            if (!AcceptSession(e.SessionId))
            {
                return;
            }

            var settings = getUiSettings();
            if (IsEnabled(settings?.MouseTrail.Enabled))
            {
                getMouseTrailForm()?.SetHighlighted(false);
            }

            if (IsEnabled(settings?.GestureHint.Enabled))
            {
                getMouseTrailForm()?.ClearGestureHint();
            }
        });
    }

    private void OnRecognized(object? sender, GestureRecognizedEventArgs e)
    {
        RunOnUi(() =>
        {
            if (!AcceptSession(e.SessionId))
            {
                return;
            }

            postWebMessage(JsonSerializer.Serialize(new
            {
                type = WebViewMessageTypes.Gesture,
                pattern = e.Pattern.Select(x => x.ToString()).ToArray(),
                action = e.ActionName
            }));

            if (IsEnabled(getUiSettings()?.GestureHint.Enabled))
            {
                ensureMouseTrailForm().ShowGestureHint(e.ActionName, e.Path[^1], autoHide: true);
            }
        });
    }

    private void OnRecordingCompleted(object? sender, GestureRecordingCompletedEventArgs e)
    {
        RunOnUi(() =>
        {
            if (!AcceptSession(e.SessionId))
            {
                return;
            }

            getMouseTrailForm()?.HideTrail();
            postWebMessage(JsonSerializer.Serialize(new
            {
                type = WebViewMessageTypes.GestureRecorded,
                requestId = e.RequestId,
                button = e.Button.ToString().ToLowerInvariant(),
                pattern = e.Pattern.Select(x => x.ToString()).ToArray()
            }));
        });
    }

    private void OnActionFailed(object? sender, GestureActionFailedEventArgs e)
    {
        RunOnUi(() =>
        {
            if (!AcceptSession(e.SessionId))
            {
                return;
            }

            postWebMessage(JsonSerializer.Serialize(new
            {
                type = WebViewMessageTypes.GestureActionFailed,
                pattern = e.Pattern.Select(x => x.ToString()).ToArray(),
                action = e.ActionName,
                error = e.Exception.Message
            }));
        });
    }

    private void OnProgressChanged(object? sender, GestureProgressEventArgs e)
    {
        RunOnUi(() =>
        {
            if (!AcceptSession(e.SessionId))
            {
                return;
            }

            var settings = getUiSettings();
            var isTrailEnabled = IsEnabled(settings?.MouseTrail.Enabled);
            var isHintEnabled = IsEnabled(settings?.GestureHint.Enabled);
            if (!isTrailEnabled && !isHintEnabled)
            {
                getMouseTrailForm()?.HideTrail();
                return;
            }

            if (!e.IsTracking || e.Path.Count < 2)
            {
                getMouseTrailForm()?.EndPath();
                return;
            }

            if (isTrailEnabled)
            {
                ensureMouseTrailForm().ShowPath(e.Path, e.Button);
            }
        });
    }

    private void RunOnUi(Action action)
    {
        if (disposed || !canUseUi())
        {
            return;
        }

        if (uiControl.InvokeRequired)
        {
            uiControl.BeginInvoke(() =>
            {
                if (!disposed && canUseUi())
                {
                    action();
                }
            });
            return;
        }

        action();
    }

    private bool AcceptSession(long sessionId)
    {
        if (sessionId < latestSessionId)
        {
            return false;
        }

        latestSessionId = sessionId;
        return true;
    }

    private static bool IsEnabled(bool? enabled) => enabled ?? true;
}
