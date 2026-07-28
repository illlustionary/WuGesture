using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureRecognizedEventArgs : EventArgs
{
    public GestureRecognizedEventArgs(
        long sessionId,
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        string actionName)
    {
        SessionId = sessionId;
        Path = path;
        Pattern = pattern;
        ActionName = actionName;
    }

    public long SessionId { get; }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public string ActionName { get; }
}
