using System.Drawing;
using System.Runtime.InteropServices;

namespace MyGesture.App.GestureEngine;

public sealed class GestureService : IDisposable
{
    private const int MinimumPointDistance = 3;
    private const int MinimumGestureDistance = 45;

    private enum ActiveMouseButton
    {
        None,
        Right,
        Middle
    }

    private readonly MouseHook mouseHook = new();
    private readonly GestureRecognizer recognizer = new();
    private GestureMatcher matcher;
    private readonly IGestureScopeContextProvider scopeContextProvider;
    private readonly ActionExecutor actionExecutor = new();
    private readonly List<Point> points = [];
    private string? recordingRequestId;
    private IReadOnlyList<GestureDirection> lastProgressPattern = [];
    private IReadOnlyList<Point> lastProgressPath = [];
    private string? lastPreviewActionName;
    private GestureScopeContext currentScopeContext = GestureScopeContext.Empty;
    private IntPtr currentTargetWindow;
    private SynchronizationContext? synchronizationContext;
    private bool isTracking;
    private bool isPaused;
    private ActiveMouseButton activeMouseButton = ActiveMouseButton.None;
    private bool started;
    private bool disposed;

    public event EventHandler<GestureRecognizedEventArgs>? GestureRecognized;

    public event EventHandler<GestureRecordingCompletedEventArgs>? GestureRecordingCompleted;

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

    public void SetPaused(bool paused)
    {
        if (disposed || isPaused == paused)
        {
            return;
        }

        isPaused = paused;
        if (!paused)
        {
            StopRecording();
            return;
        }

        CancelTracking();
    }

    public void StartRecording(string requestId)
    {
        if (disposed)
        {
            return;
        }

        var trimmed = requestId.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        CancelTracking();
        recordingRequestId = trimmed;
        ClearPreviewMatch();
    }

    public void StopRecording()
    {
        if (recordingRequestId is null)
        {
            return;
        }

        recordingRequestId = null;
        if (isTracking)
        {
            CancelTracking();
            return;
        }

        ClearPreviewMatch();
    }

    public void Start()
    {
        if (disposed || started)
        {
            return;
        }

        synchronizationContext = SynchronizationContext.Current;
        mouseHook.RightButtonDown += OnRightButtonDown;
        mouseHook.MiddleButtonDown += OnMiddleButtonDown;
        mouseHook.MouseMove += OnMouseMove;
        mouseHook.RightButtonUp += OnRightButtonUp;
        mouseHook.MiddleButtonUp += OnMiddleButtonUp;
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
        activeMouseButton = ActiveMouseButton.None;
        mouseHook.RightButtonDown -= OnRightButtonDown;
        mouseHook.MiddleButtonDown -= OnMiddleButtonDown;
        mouseHook.MouseMove -= OnMouseMove;
        mouseHook.RightButtonUp -= OnRightButtonUp;
        mouseHook.MiddleButtonUp -= OnMiddleButtonUp;
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
        StartTracking(e, ActiveMouseButton.Right, swallowInput: true);
    }

    private void OnMiddleButtonDown(object? sender, MouseHookEventArgs e)
    {
        StartTracking(e, ActiveMouseButton.Middle, swallowInput: true);
    }

    private void StartTracking(MouseHookEventArgs e, ActiveMouseButton button, bool swallowInput)
    {
        if (disposed || isTracking || (isPaused && recordingRequestId is null))
        {
            return;
        }

        e.Handled = swallowInput;
        activeMouseButton = button;
        points.Clear();
        points.Add(e.Location);
        isTracking = true;
        currentScopeContext = recordingRequestId is null
            ? scopeContextProvider.GetCurrentContext()
            : GestureScopeContext.Empty;
        currentTargetWindow = recordingRequestId is null ? GetForegroundWindow() : IntPtr.Zero;
        lastProgressPattern = [];
        lastProgressPath = [];
        lastPreviewActionName = null;
        RaiseProgress(points.ToArray(), [], true, ToPublicButton(button), force: true);
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
        if (disposed || !isTracking || activeMouseButton != ActiveMouseButton.Right)
        {
            return;
        }

        e.Handled = true;
        FinishTracking(e.Location, ActiveMouseButton.Right);
    }

    private void OnMiddleButtonUp(object? sender, MouseHookEventArgs e)
    {
        if (disposed || !isTracking || activeMouseButton != ActiveMouseButton.Middle)
        {
            return;
        }

        e.Handled = true;
        FinishTracking(e.Location, ActiveMouseButton.Middle);
    }

    private void CancelTracking()
    {
        var button = activeMouseButton;
        var path = points.ToArray();

        isTracking = false;
        activeMouseButton = ActiveMouseButton.None;
        points.Clear();
        lastProgressPattern = [];
        lastProgressPath = [];
        ClearPreviewMatch();
        RaiseProgress(path, [], false, ToPublicButton(button), force: true);
    }

    private void FinishTracking(Point location, ActiveMouseButton button)
    {
        var recordingId = recordingRequestId;
        recordingRequestId = null;
        isTracking = false;
        activeMouseButton = ActiveMouseButton.None;
        points.Add(location);
        var path = points.ToArray();

        if (recordingId is not null)
        {
            var recordingPattern = recognizer.Recognize(points);
            RaiseProgress(path, recordingPattern, false, ToPublicButton(button), force: true);
            ClearPreviewMatch();
            Post(() =>
            {
                if (disposed)
                {
                    return;
                }

                GestureRecordingCompleted?.Invoke(
                    this,
                    new GestureRecordingCompletedEventArgs(recordingId, path, recordingPattern, ToPublicButton(button)));
            });
            return;
        }

        if (points.Count < 2 || Distance(points[0], points[^1]) < MinimumGestureDistance)
        {
            RaiseProgress(path, [], false, ToPublicButton(button), force: true);
            if (button == ActiveMouseButton.Right)
            {
                Post(MouseInput.ReplayRightClick);
            }
            else if (button == ActiveMouseButton.Middle)
            {
                Post(MouseInput.ReplayMiddleClick);
            }

            return;
        }

        var publicButton = ToPublicButton(button);
        var pattern = recognizer.Recognize(points);
        var rule = matcher.Match(pattern, currentScopeContext, publicButton);
        if (rule is null)
        {
            RaiseProgress(path, pattern, false, publicButton, force: true);
            return;
        }

        RaiseProgress(path, pattern, false, publicButton, force: true);
        Post(() =>
        {
            if (disposed)
            {
                return;
            }

            try
            {
                GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(path, pattern, rule.ActionName));
                actionExecutor.Execute(rule, currentTargetWindow);
            }
            catch (Exception exception)
            {
                GestureActionFailed?.Invoke(this, new GestureActionFailedEventArgs(path, pattern, rule.ActionName, exception));
            }
        });
    }

    private void PublishProgress()
    {
        if (recordingRequestId is not null)
        {
            RaiseProgress(points.ToArray(), [], true, ToPublicButton(activeMouseButton));
            return;
        }

        var pattern = recognizer.Recognize(points);
        var path = points.ToArray();
        RaiseProgress(path, pattern, true, ToPublicButton(activeMouseButton));

        RaisePreviewMatch(path, pattern, currentScopeContext, ToPublicButton(activeMouseButton));
    }

    private void RaisePreviewMatch(
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        GestureScopeContext context,
        GestureMouseButton button)
    {
        var rule = matcher.Match(pattern, context, button);
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
        GestureMouseButton button,
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
                    new GestureProgressEventArgs(path, pattern, isCurrentlyTracking, button));
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

    private static GestureMouseButton ToPublicButton(ActiveMouseButton button)
    {
        return button == ActiveMouseButton.Middle ? GestureMouseButton.Middle : GestureMouseButton.Right;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
