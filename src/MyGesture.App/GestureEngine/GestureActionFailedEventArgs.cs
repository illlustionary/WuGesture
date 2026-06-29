using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureActionFailedEventArgs : EventArgs
{
    public GestureActionFailedEventArgs(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        string actionName,
        Exception exception)
    {
        Path = path;
        Pattern = pattern;
        ActionName = actionName;
        Exception = exception;
    }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public string ActionName { get; }

    public Exception Exception { get; }
}
