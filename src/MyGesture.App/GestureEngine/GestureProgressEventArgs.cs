using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureProgressEventArgs : EventArgs
{
    public GestureProgressEventArgs(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        bool isTracking)
    {
        Path = path;
        Pattern = pattern;
        IsTracking = isTracking;
    }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public bool IsTracking { get; }
}
