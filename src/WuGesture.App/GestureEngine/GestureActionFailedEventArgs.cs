using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureActionFailedEventArgs : EventArgs
{
    public GestureActionFailedEventArgs(
        long sessionId,
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        string actionName,
        Exception exception)
    {
        SessionId = sessionId;
        Path = path;
        Pattern = pattern;
        ActionName = actionName;
        Exception = exception;
    }

    public long SessionId { get; }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public string ActionName { get; }

    public Exception Exception { get; }
}
