using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureProgressEventArgs : EventArgs
{
    public GestureProgressEventArgs(
        long sessionId,
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        bool isTracking,
        GestureMouseButton button,
        long? sourceTimestamp = null,
        bool hasWheelModifier = false)
    {
        SessionId = sessionId;
        Path = path;
        Pattern = pattern;
        IsTracking = isTracking;
        Button = button;
        SourceTimestamp = sourceTimestamp;
        HasWheelModifier = hasWheelModifier;
    }

    public long SessionId { get; }

    public IReadOnlyList<Point> Path { get; }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public bool IsTracking { get; }

    public GestureMouseButton Button { get; }

    public long? SourceTimestamp { get; }

    /// <summary>
    /// True when the current session already consumed a wheel notch. The overlay uses this to
    /// keep the wheel hint attached to the latest path end without re-selecting the rule.
    /// </summary>
    public bool HasWheelModifier { get; }
}
