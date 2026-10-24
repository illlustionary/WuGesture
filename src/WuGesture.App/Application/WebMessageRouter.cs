using System.Text.Json;

namespace WuGesture.App;

/// <summary>
/// Parses one inbound WebView message and dispatches it to the matching handler.
/// Failures are contained here, so a malformed payload can no longer reach the WinForms message loop.
/// </summary>
internal sealed class WebMessageRouter
{
    private readonly IReadOnlyDictionary<string, Action<string>> handlers;
    private readonly Action<string?> onStatusRequest;
    private readonly Action<Exception> onFailure;

    public WebMessageRouter(
        IReadOnlyDictionary<string, Action<string>> handlers,
        Action<string?> onStatusRequest,
        Action<Exception> onFailure)
    {
        this.handlers = handlers;
        this.onStatusRequest = onStatusRequest;
        this.onFailure = onFailure;
    }

    public void Route(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                onStatusRequest(root.GetString());
                return;
            }

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out var typeElement))
            {
                return;
            }

            var messageType = typeElement.GetString();
            if (string.IsNullOrEmpty(messageType))
            {
                return;
            }

            if (!handlers.TryGetValue(messageType, out var handler))
            {
                AppLogger.Warning(
                    "WebMessageRouter",
                    "unknown-message",
                    $"Ignoring an unknown WebView message type: {messageType}.");
                return;
            }

            handler(json);
        }
        catch (Exception exception)
        {
            AppLogger.Error("WebMessageRouter", "message-failed", "Unable to process a WebView message.", exception);
            onFailure(exception);
        }
    }
}
