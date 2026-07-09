using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureRecordingCompletedEventArgs : EventArgs
{
    public GestureRecordingCompletedEventArgs(
        string requestId,
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        GestureMouseButton button)
    {
        RequestId = requestId;
        Path = path;
        Pattern = pattern;
        Button = button;
    }

    public string RequestId { get; }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public GestureMouseButton Button { get; }
}
