using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureService : IDisposable
{
    private const int MinimumPointDistance = 8;
    private const int MinimumGestureDistance = 45;

    private readonly MouseHook mouseHook = new();
    private readonly GestureRecognizer recognizer = new();
    private GestureMatcher matcher;
    private readonly ActionExecutor actionExecutor = new();
    private readonly List<Point> points = [];
    private IReadOnlyList<GestureDirection> lastProgressPattern = [];
    private SynchronizationContext? synchronizationContext;
    private bool isTracking;
    private bool started;
    private bool disposed;

    public event EventHandler<GestureRecognizedEventArgs>? GestureRecognized;

    public event EventHandler<GestureProgressEventArgs>? GestureProgressChanged;

    public event EventHandler<GestureActionFailedEventArgs>? GestureActionFailed;

    public GestureService(GestureMatcher matcher)
    {
        this.matcher = matcher;
    }

    public void UpdateMatcher(GestureMatcher newMatcher)
    {
        matcher = newMatcher;
    }

    public void Start()
    {
        if (disposed || started)
        {
            return;
        }

        synchronizationContext = SynchronizationContext.Current;
        mouseHook.RightButtonDown += OnRightButtonDown;
        mouseHook.MouseMove += OnMouseMove;
        mouseHook.RightButtonUp += OnRightButtonUp;
        mouseHook.Start();
        started = true;
    }

    public void Stop()
    {
        if (!started)
        {
            return;
        }

        isTracking = false;
        mouseHook.RightButtonDown -= OnRightButtonDown;
        mouseHook.MouseMove -= OnMouseMove;
        mouseHook.RightButtonUp -= OnRightButtonUp;
        mouseHook.Dispose();
        started = false;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Stop();
    }

    private void OnRightButtonDown(object? sender, MouseHookEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        e.Handled = true;
        points.Clear();
        points.Add(e.Location);
        isTracking = true;
        lastProgressPattern = [];
        RaiseProgress([], true, force: true);
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        if (disposed || !isTracking)
        {
            return;
        }

        var lastPoint = points[^1];
        if (Distance(lastPoint, e.Location) < MinimumPointDistance)
        {
            return;
        }

        points.Add(e.Location);
        PublishProgress();
    }

    private void OnRightButtonUp(object? sender, MouseHookEventArgs e)
    {
        if (disposed || !isTracking)
        {
            return;
        }

        e.Handled = true;
        isTracking = false;
        points.Add(e.Location);

        if (points.Count < 2 || Distance(points[0], points[^1]) < MinimumGestureDistance)
        {
            RaiseProgress([], false, force: true);
            Post(MouseInput.ReplayRightClick);
            return;
        }

        var pattern = recognizer.Recognize(points);
        var rule = matcher.Match(pattern);
        if (rule is null)
        {
            RaiseProgress(pattern, false, force: true);
            return;
        }

        RaiseProgress(pattern, false, force: true);
        Post(() =>
        {
            if (disposed)
            {
                return;
            }

            try
            {
                actionExecutor.Execute(rule);
                GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(pattern, rule.ActionName));
            }
            catch (Exception exception)
            {
                GestureActionFailed?.Invoke(this, new GestureActionFailedEventArgs(pattern, rule.ActionName, exception));
            }
        });
    }

    private void PublishProgress()
    {
        var pattern = recognizer.Recognize(points);
        RaiseProgress(pattern, true);
    }

    private void RaiseProgress(IReadOnlyList<GestureDirection> pattern, bool isCurrentlyTracking, bool force = false)
    {
        if (!force && lastProgressPattern.SequenceEqual(pattern))
        {
            return;
        }

        lastProgressPattern = pattern.ToArray();
        Post(() =>
        {
            if (!disposed)
            {
                GestureProgressChanged?.Invoke(this, new GestureProgressEventArgs(pattern, isCurrentlyTracking));
            }
        });
    }

    private void Post(Action action)
    {
        var context = synchronizationContext;
        if (context is null)
        {
            action();
            return;
        }

        context.Post(_ => action(), null);
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
