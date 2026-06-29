using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureService : IDisposable
{
    private const int MinimumPointDistance = 8;
    private const int MinimumGestureDistance = 45;

    private readonly MouseHook mouseHook = new();
    private readonly GestureRecognizer recognizer = new();
    private GestureMatcher matcher;
    private readonly IGestureScopeContextProvider scopeContextProvider;
    private readonly ActionExecutor actionExecutor = new();
    private readonly List<Point> points = [];
    private IReadOnlyList<GestureDirection> lastProgressPattern = [];
    private IReadOnlyList<Point> lastProgressPath = [];
    private string? lastPreviewActionName;
    private GestureScopeContext currentScopeContext = GestureScopeContext.Empty;
    private SynchronizationContext? synchronizationContext;
    private bool isTracking;
    private bool started;
    private bool disposed;

    public event EventHandler<GestureRecognizedEventArgs>? GestureRecognized;

    public event EventHandler<GestureRecognizedEventArgs>? GesturePreviewMatched;

    public event EventHandler? GesturePreviewCleared;

    public event EventHandler<GestureProgressEventArgs>? GestureProgressChanged;

    public event EventHandler<GestureActionFailedEventArgs>? GestureActionFailed;

    public GestureService(GestureMatcher matcher, IGestureScopeContextProvider? scopeContextProvider = null)
    {
        this.matcher = matcher;
        this.scopeContextProvider = scopeContextProvider ?? new ForegroundWindowScopeContextProvider();
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
        currentScopeContext = scopeContextProvider.GetCurrentContext();
        lastProgressPattern = [];
        lastProgressPath = [];
        lastPreviewActionName = null;
        RaiseProgress(points.ToArray(), [], true, force: true);
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
        var path = points.ToArray();

        if (points.Count < 2 || Distance(points[0], points[^1]) < MinimumGestureDistance)
        {
            RaiseProgress(path, [], false, force: true);
            Post(MouseInput.ReplayRightClick);
            return;
        }

        var pattern = recognizer.Recognize(points);
        var rule = matcher.Match(pattern, currentScopeContext);
        if (rule is null)
        {
            RaiseProgress(path, pattern, false, force: true);
            return;
        }

        RaiseProgress(path, pattern, false, force: true);
        Post(() =>
        {
            if (disposed)
            {
                return;
            }

            try
            {
                GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(path, pattern, rule.ActionName));
                actionExecutor.Execute(rule);
            }
            catch (Exception exception)
            {
                GestureActionFailed?.Invoke(this, new GestureActionFailedEventArgs(path, pattern, rule.ActionName, exception));
            }
        });
    }

    private void PublishProgress()
    {
        var pattern = recognizer.Recognize(points);
        var path = points.ToArray();
        RaiseProgress(path, pattern, true);
        RaisePreviewMatch(path, pattern, currentScopeContext);
    }

    private void RaisePreviewMatch(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        GestureScopeContext context)
    {
        var rule = matcher.Match(pattern, context);
        if (rule is null)
        {
            ClearPreviewMatch();
            return;
        }

        if (rule.ActionName == lastPreviewActionName)
        {
            return;
        }

        lastPreviewActionName = rule.ActionName;
        Post(() =>
        {
            if (!disposed)
            {
                GesturePreviewMatched?.Invoke(this, new GestureRecognizedEventArgs(path, pattern, rule.ActionName));
            }
        });
    }

    private void ClearPreviewMatch()
    {
        if (lastPreviewActionName is null)
        {
            return;
        }

        lastPreviewActionName = null;
        Post(() =>
        {
            if (!disposed)
            {
                GesturePreviewCleared?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void RaiseProgress(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        bool isCurrentlyTracking,
        bool force = false)
    {
        if (!force &&
            lastProgressPattern.SequenceEqual(pattern) &&
            lastProgressPath.SequenceEqual(path))
        {
            return;
        }

        lastProgressPattern = pattern.ToArray();
        lastProgressPath = path.ToArray();
        Post(() =>
        {
            if (!disposed)
            {
                GestureProgressChanged?.Invoke(
                    this,
                    new GestureProgressEventArgs(path, pattern, isCurrentlyTracking));
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
