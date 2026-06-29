using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureRecognizedEventArgs : EventArgs
{
    public GestureRecognizedEventArgs(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        string actionName)
    {
        Path = path;
        Pattern = pattern;
        ActionName = actionName;
    }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public string ActionName { get; }
}
