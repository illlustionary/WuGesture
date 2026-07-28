using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureProgressEventArgs : EventArgs
{
    public GestureProgressEventArgs(
        long sessionId,
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        bool isTracking,
        GestureMouseButton button)
    {
        SessionId = sessionId;
        Path = path;
        Pattern = pattern;
        IsTracking = isTracking;
        Button = button;
    }

    public long SessionId { get; }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public bool IsTracking { get; }

    public GestureMouseButton Button { get; }
}
