using System.Drawing;
using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

public sealed class GestureService : IDisposable
{
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
    private readonly ApplicationExclusionMatcher exclusionMatcher = new();
    private readonly List<Point> points = [];
    private int minimumGestureDistance = GestureRuntimeDefaults.MinimumGestureDistance;
    private string? recordingRequestId;
    private IReadOnlyList<GestureDirection> lastProgressPattern = [];
    private IReadOnlyList<Point> lastProgressPath = [];
    private string? lastPreviewActionName;
    private long lastPreviewSessionId;
    private string targetWindowMode = GestureConfigContract.WindowTargetModes.StartWindow;
    private GestureExecutionContext activeGestureContext = GestureExecutionContext.Empty;
    private SynchronizationContext? synchronizationContext;
    private bool isTracking;
    private bool isPaused;
    private bool disableGesturesInFullscreen;
    private ActiveMouseButton activeMouseButton = ActiveMouseButton.None;
    private ActiveMouseButton suppressedReleaseButton = ActiveMouseButton.None;
    private long nextSessionId;
    private long activeSessionId;
    private bool isCompleting;
    private bool started;
    private bool disposed;

    private sealed record GestureExecutionContext(
        string TargetWindowMode,
        IntPtr StartWindow,
        GestureScopeContext ScopeContext)
    {
        public static GestureExecutionContext Empty { get; } = new(
            GestureConfigContract.WindowTargetModes.StartWindow,
            IntPtr.Zero,
            GestureScopeContext.Empty);

        public bool UsesStartWindow =>
            TargetWindowMode == GestureConfigContract.WindowTargetModes.StartWindow;
    }

    public event EventHandler<GestureRecognizedEventArgs>? GestureRecognized;

    public event EventHandler<GestureRecordingCompletedEventArgs>? GestureRecordingCompleted;

    public event EventHandler<GestureRecognizedEventArgs>? GesturePreviewMatched;

    public event EventHandler<GesturePreviewClearedEventArgs>? GesturePreviewCleared;

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

    public void ApplyGestureSensitivity(GestureSensitivityUiSettings settings)
    {
        recognizer.ApplySensitivity(settings.Percent);
        minimumGestureDistance = recognizer.MinimumGestureDistance;
    }

    public void UpdateExcludedApplications(IEnumerable<ExcludedApplicationConfig>? applications)
    {
        exclusionMatcher.Update(applications);
        if (isTracking && recordingRequestId is null && IsGestureExcluded(activeGestureContext))
        {
            CancelTracking();
        }
    }

    public void UpdateFullscreenBehavior(bool disableGestures)
    {
        disableGesturesInFullscreen = disableGestures;
        if (disableGesturesInFullscreen && ForegroundWindowFullscreenDetector.IsFullscreenForegroundWindow())
        {
            CancelTracking();
        }
    }

    public void UpdateTargetWindowMode(string? mode)
    {
        targetWindowMode = mode == GestureConfigContract.WindowTargetModes.CurrentWindow
            ? GestureConfigContract.WindowTargetModes.CurrentWindow
            : GestureConfigContract.WindowTargetModes.StartWindow;
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

        CancelTracking();
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
        if (disposed || (isPaused && recordingRequestId is null) || isCompleting)
        {
            return;
        }

        if (isTracking)
        {
            var previousButton = activeMouseButton;
            CancelTracking();
            if (previousButton != button)
            {
                // The original Down was swallowed, so swallow its later Up if it arrives.
                suppressedReleaseButton = previousButton;
            }
        }

        var gestureContext = recordingRequestId is null
            ? CreateGestureContext(e.Location)
            : GestureExecutionContext.Empty;

        if (recordingRequestId is null &&
            (IsGestureExcluded(gestureContext) || IsDisabledInFullscreen()))
        {
            return;
        }

        e.Handled = swallowInput;
        if (suppressedReleaseButton == button)
        {
            suppressedReleaseButton = ActiveMouseButton.None;
        }

        activeMouseButton = button;
        activeSessionId = ++nextSessionId;
        points.Clear();
        points.Add(e.Location);
        isTracking = true;
        activeGestureContext = gestureContext;
        lastProgressPattern = [];
        lastProgressPath = [];
        lastPreviewActionName = null;
        lastPreviewSessionId = 0;
        RaiseProgress(activeSessionId, points.ToArray(), [], true, ToPublicButton(button), force: true);
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        if (disposed || !isTracking)
        {
            return;
        }

        if (recordingRequestId is null && IsDisabledInFullscreen())
        {
            CancelTracking();
            return;
        }

        var lastPoint = points[^1];
        if (Distance(lastPoint, e.Location) < GestureRuntimeDefaults.MinimumPointDistance)
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
            SuppressAbandonedButtonUp(e, ActiveMouseButton.Right);
            return;
        }

        e.Handled = true;
        FinishTracking(e.Location, ActiveMouseButton.Right);
    }

    private void OnMiddleButtonUp(object? sender, MouseHookEventArgs e)
    {
        if (disposed || !isTracking || activeMouseButton != ActiveMouseButton.Middle)
        {
            SuppressAbandonedButtonUp(e, ActiveMouseButton.Middle);
            return;
        }

        e.Handled = true;
        FinishTracking(e.Location, ActiveMouseButton.Middle);
    }

    private void CancelTracking()
    {
        if (!isTracking && !isCompleting)
        {
            return;
        }

        var button = activeMouseButton;
        var sessionId = activeSessionId;
        var path = points.ToArray();

        isTracking = false;
        isCompleting = false;
        activeMouseButton = ActiveMouseButton.None;
        activeSessionId = 0;
        activeGestureContext = GestureExecutionContext.Empty;
        points.Clear();
        lastProgressPattern = [];
        lastProgressPath = [];
        ClearPreviewMatch(sessionId);
        RaiseProgress(sessionId, path, [], false, ToPublicButton(button), force: true);
    }

    private void FinishTracking(Point location, ActiveMouseButton button)
    {
        var sessionId = activeSessionId;
        var recordingId = recordingRequestId;
        var gestureContext = activeGestureContext;
        recordingRequestId = null;
        isTracking = false;
        isCompleting = true;
        activeMouseButton = ActiveMouseButton.None;
        activeGestureContext = GestureExecutionContext.Empty;
        points.Add(location);
        var path = points.ToArray();

        if (recordingId is not null)
        {
            var recordingPattern = recognizer.Recognize(points);
            RaiseProgress(sessionId, path, recordingPattern, false, ToPublicButton(button), force: true);
            ClearPreviewMatch(sessionId);
            Post(() =>
            {
                if (!CanCompleteSession(sessionId))
                {
                    return;
                }

                GestureRecordingCompleted?.Invoke(
                    this,
                    new GestureRecordingCompletedEventArgs(sessionId, recordingId, path, recordingPattern, ToPublicButton(button)));
                CompleteSession(sessionId);
            });
            return;
        }

        if (points.Count < 2 || PathLength(points) < minimumGestureDistance)
        {
            RaiseProgress(sessionId, path, [], false, ToPublicButton(button), force: true);
            if (button == ActiveMouseButton.Right)
            {
                Post(() =>
                {
                    if (!CanCompleteSession(sessionId))
                    {
                        return;
                    }

                    MouseInput.ReplayRightClick();
                    CompleteSession(sessionId);
                });
            }
            else if (button == ActiveMouseButton.Middle)
            {
                Post(() =>
                {
                    if (!CanCompleteSession(sessionId))
                    {
                        return;
                    }

                    MouseInput.ReplayMiddleClick();
                    CompleteSession(sessionId);
                });
            }

            return;
        }

        var publicButton = ToPublicButton(button);
        var pattern = recognizer.Recognize(points);
        var rule = matcher.Match(pattern, gestureContext.ScopeContext, publicButton);
        if (rule is null)
        {
            RaiseProgress(sessionId, path, pattern, false, publicButton, force: true);
            Post(() => CompleteSession(sessionId));
            return;
        }

        RaiseProgress(sessionId, path, pattern, false, publicButton, force: true);
        Post(() =>
        {
            if (!CanCompleteSession(sessionId))
            {
                return;
            }

            try
            {
                GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(sessionId, path, pattern, rule.ActionName));
                var isDesktopCloseAction = rule.Action is WindowControlAction
                {
                    Operation: WindowControlOperation.Close
                };
                var targetWindow = gestureContext.UsesStartWindow
                    ? gestureContext.StartWindow
                    : GetForegroundWindow();
                var isDesktopCloseTarget = isDesktopCloseAction &&
                                           targetWindow != IntPtr.Zero &&
                                           DesktopWindowClassifier.IsDesktopSurface(targetWindow);
                if (gestureContext.UsesStartWindow)
                {
                    if (gestureContext.StartWindow == IntPtr.Zero)
                    {
                        return;
                    }

                    if (!isDesktopCloseTarget)
                    {
                        SetForegroundWindow(gestureContext.StartWindow);
                    }
                }

                actionExecutor.Execute(
                    rule,
                    targetWindow,
                    targetIsDesktopSurface: isDesktopCloseTarget);
            }
            catch (Exception exception)
            {
                GestureActionFailed?.Invoke(this, new GestureActionFailedEventArgs(sessionId, path, pattern, rule.ActionName, exception));
            }
            finally
            {
                CompleteSession(sessionId);
            }
        });
    }

    private void SuppressAbandonedButtonUp(MouseHookEventArgs e, ActiveMouseButton button)
    {
        if (suppressedReleaseButton == button)
        {
            e.Handled = true;
            suppressedReleaseButton = ActiveMouseButton.None;
        }
    }

    private bool CanCompleteSession(long sessionId)
    {
        return !disposed && isCompleting && activeSessionId == sessionId;
    }

    private void CompleteSession(long sessionId)
    {
        if (activeSessionId == sessionId)
        {
            isCompleting = false;
            activeSessionId = 0;
        }
    }

    private void PublishProgress()
    {
        if (recordingRequestId is not null)
        {
            RaiseProgress(activeSessionId, points.ToArray(), [], true, ToPublicButton(activeMouseButton));
            return;
        }

        var pattern = recognizer.Recognize(points);
        var path = points.ToArray();
        RaiseProgress(activeSessionId, path, pattern, true, ToPublicButton(activeMouseButton));

        RaisePreviewMatch(activeSessionId, path, pattern, activeGestureContext.ScopeContext, ToPublicButton(activeMouseButton));
    }

    private void RaisePreviewMatch(
        long sessionId,
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        GestureScopeContext context,
        GestureMouseButton button)
    {
        var rule = matcher.Match(pattern, context, button);
        if (rule is null)
        {
            ClearPreviewMatch(sessionId);
            return;
        }

        if (rule.ActionName == lastPreviewActionName)
        {
            return;
        }

        lastPreviewActionName = rule.ActionName;
        lastPreviewSessionId = sessionId;
        Post(() =>
        {
            if (!disposed)
            {
                GesturePreviewMatched?.Invoke(this, new GestureRecognizedEventArgs(sessionId, path, pattern, rule.ActionName));
            }
        });
    }

    private void ClearPreviewMatch(long? sessionId = null)
    {
        if (lastPreviewActionName is null)
        {
            return;
        }

        var previewSessionId = sessionId ?? lastPreviewSessionId;
        lastPreviewActionName = null;
        lastPreviewSessionId = 0;
        Post(() =>
        {
            if (!disposed)
            {
                GesturePreviewCleared?.Invoke(this, new GesturePreviewClearedEventArgs(previewSessionId));
            }
        });
    }

    private void RaiseProgress(
        long sessionId,
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
                    new GestureProgressEventArgs(sessionId, path, pattern, isCurrentlyTracking, button));
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

    private bool IsDisabledInFullscreen()
    {
        return disableGesturesInFullscreen && ForegroundWindowFullscreenDetector.IsFullscreenForegroundWindow();
    }

    private GestureExecutionContext CreateGestureContext(Point startLocation)
    {
        var mode = targetWindowMode;
        if (mode == GestureConfigContract.WindowTargetModes.StartWindow)
        {
            var startTarget = ResolveStartTargetWindow(startLocation);
            return new GestureExecutionContext(
                mode,
                startTarget,
                scopeContextProvider.GetContextForWindow(startTarget));
        }

        return new GestureExecutionContext(
            mode,
            IntPtr.Zero,
            scopeContextProvider.GetCurrentContext());
    }

    private bool IsGestureExcluded(GestureExecutionContext context)
    {
        return context.UsesStartWindow
            ? exclusionMatcher.IsGestureExcluded(context.StartWindow)
            : exclusionMatcher.IsGestureExcluded();
    }

    private static IntPtr ResolveStartTargetWindow(Point location)
    {
        var window = WindowFromPoint(location);
        if (window == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var rootWindow = GetAncestor(window, GetAncestorRoot);
        return rootWindow != IntPtr.Zero ? rootWindow : window;
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double PathLength(IReadOnlyList<Point> path)
    {
        var length = 0.0;
        for (var i = 1; i < path.Count; i++)
        {
            length += Distance(path[i - 1], path[i]);
        }

        return length;
    }

    private static GestureMouseButton ToPublicButton(ActiveMouseButton button)
    {
        return button == ActiveMouseButton.Middle ? GestureMouseButton.Middle : GestureMouseButton.Right;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    private const uint GetAncestorRoot = 2;
}
