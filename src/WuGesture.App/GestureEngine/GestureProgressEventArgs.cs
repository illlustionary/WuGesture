using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureProgressEventArgs : EventArgs
{
    public GestureProgressEventArgs(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        bool isTracking,
        GestureMouseButton button)
    {
        Path = path;
        Pattern = pattern;
        IsTracking = isTracking;
        Button = button;
    }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public bool IsTracking { get; }

    public GestureMouseButton Button { get; }
}
