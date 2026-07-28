using System.Drawing;

namespace WuGesture.App.GestureEngine;

internal sealed class GestureSession
{
    private readonly List<Point> points = [];

    public long Id { get; private set; }

    public bool IsTracking { get; private set; }

    public bool IsCompleting { get; private set; }

    public IReadOnlyList<Point> Points => points;

    public Point LastPoint => points[^1];

    public IReadOnlyList<GestureDirection> LastProgressPattern { get; private set; } = [];

    public IReadOnlyList<Point> LastProgressPath { get; private set; } = [];

    public string? PreviewActionName { get; set; }

    public long PreviewSessionId { get; set; }

    public void Start(long id, Point startPoint)
    {
        Id = id;
        IsTracking = true;
        IsCompleting = false;
        points.Clear();
        points.Add(startPoint);
        LastProgressPattern = [];
        LastProgressPath = [];
        PreviewActionName = null;
        PreviewSessionId = 0;
    }

    public void Append(Point point) => points.Add(point);

    public IReadOnlyList<Point> Snapshot() => points.ToArray();

    public void BeginCompleting()
    {
        IsTracking = false;
        IsCompleting = true;
    }

    public void Cancel()
    {
        IsTracking = false;
        IsCompleting = false;
        Id = 0;
        points.Clear();
        LastProgressPattern = [];
        LastProgressPath = [];
    }

    public bool CanComplete(long sessionId) => IsCompleting && Id == sessionId;

    public void Complete(long sessionId)
    {
        if (Id == sessionId)
        {
            Cancel();
        }
    }

    public bool ShouldPublishProgress(
        IReadOnlyList<GestureDirection> pattern,
        IReadOnlyList<Point> path,
        bool force)
    {
        if (!force &&
            LastProgressPattern.SequenceEqual(pattern) &&
            LastProgressPath.SequenceEqual(path))
        {
            return false;
        }

        LastProgressPattern = pattern.ToArray();
        LastProgressPath = path.ToArray();
        return true;
    }

    public long ClearPreview(long? sessionId = null)
    {
        var previewSessionId = sessionId ?? PreviewSessionId;
        PreviewActionName = null;
        PreviewSessionId = 0;
        return previewSessionId;
    }
}
