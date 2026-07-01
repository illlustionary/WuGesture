using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureRecordedEventArgs : EventArgs
{
    public GestureRecordedEventArgs(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        GestureMouseButton button)
    {
        Path = path;
        Pattern = pattern;
        Button = button;
    }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public GestureMouseButton Button { get; }
}
