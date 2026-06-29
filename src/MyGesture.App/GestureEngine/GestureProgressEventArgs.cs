namespace MyGesture.App.GestureEngine;

public sealed class GestureProgressEventArgs : EventArgs
{
    public GestureProgressEventArgs(IReadOnlyList<GestureDirection> pattern, bool isTracking)
    {
        Pattern = pattern;
        IsTracking = isTracking;
    }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public bool IsTracking { get; }
}
