using System.Collections.Concurrent;
using System.Drawing;
using System.Diagnostics;
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

    private readonly object stateLock = new();
    private readonly MouseHook mouseHook = new();
    private readonly GestureRecognizer recognizer = new();
    private GestureMatcher matcher;
    private readonly IGestureScopeContextProvider scopeContextProvider;
    private readonly ActionExecutor actionExecutor = new();
    private readonly ApplicationExclusionMatcher exclusionMatcher = new();
    private readonly GestureSession session = new();
    private readonly ConcurrentQueue<PendingAction> pendingActions = new();
    private GestureProgressEventArgs? pendingTrackingProgress;
    private int minimumGestureDistance = GestureRuntimeDefaults.MinimumGestureDistance;
    private int actionWorkerScheduled;
    private int trackingProgressPostScheduled;
    private string? recordingRequestId;
    private string targetWindowMode = GestureConfigContract.WindowTargetModes.StartWindow;
    private GestureExecutionContext activeGestureContext = GestureExecutionContext.Empty;
    private SynchronizationContext? synchronizationContext;
    private bool isPaused;
    private bool disableGesturesInFullscreen;
    private ActiveMouseButton activeMouseButton = ActiveMouseButton.None;
    private ActiveMouseButton suppressedReleaseButton = ActiveMouseButton.None;
    private long nextSessionId;
    private bool started;
    private volatile bool disposed;

    private sealed record PendingAction(
        long SessionId,
        IReadOnlyList<Point> Path,
        IReadOnlyList<GestureDirection> Pattern,
        GestureRule Rule,
        IntPtr TargetWindow,
        bool ActivateTargetWindow);

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
        lock (stateLock)
        {
            matcher = newMatcher;
        }
    }

    public void ApplyGestureSensitivity(GestureSensitivityUiSettings settings)
    {
        lock (stateLock)
        {
            recognizer.ApplySensitivity(settings.Percent);
            minimumGestureDistance = recognizer.MinimumGestureDistance;
        }
    }

    public void UpdateExcludedApplications(IEnumerable<ExcludedApplicationConfig>? applications)
    {
        lock (stateLock)
        {
            exclusionMatcher.Update(applications);
            if (session.IsTracking && recordingRequestId is null && IsGestureExcluded(activeGestureContext))
            {
                CancelTracking();
            }
        }
    }

    public void UpdateFullscreenBehavior(bool disableGestures)
    {
        lock (stateLock)
        {
            disableGesturesInFullscreen = disableGestures;
            if (disableGesturesInFullscreen && ForegroundWindowFullscreenDetector.IsFullscreenForegroundWindow())
            {
                CancelTracking();
            }
        }
    }

    public void UpdateTargetWindowMode(string? mode)
    {
        lock (stateLock)
        {
            targetWindowMode = mode == GestureConfigContract.WindowTargetModes.CurrentWindow
                ? GestureConfigContract.WindowTargetModes.CurrentWindow
                : GestureConfigContract.WindowTargetModes.StartWindow;
        }
    }

    public void SetPaused(bool paused)
    {
        lock (stateLock)
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
    }

    public void StartRecording(string requestId)
    {
        lock (stateLock)
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
    }

    public void StopRecording()
    {
        lock (stateLock)
        {
            if (recordingRequestId is null)
            {
                return;
            }

            recordingRequestId = null;
            if (session.IsTracking)
            {
                CancelTracking();
                return;
            }

            ClearPreviewMatch();
        }
    }

    public void Start()
    {
        lock (stateLock)
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
            GestureStartDiagnostics.ReportServiceStarted();
            started = true;
        }
    }

    public void Stop()
    {
        lock (stateLock)
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
            started = false;
        }

        mouseHook.Dispose();
    }

    public void Dispose()
    {
        lock (stateLock)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
        }

        Stop();
    }

    private void OnRightButtonDown(object? sender, MouseHookEventArgs e)
    {
        lock (stateLock)
        {
            StartTracking(e, ActiveMouseButton.Right, swallowInput: true);
        }
    }

    private void OnMiddleButtonDown(object? sender, MouseHookEventArgs e)
    {
        lock (stateLock)
        {
            StartTracking(e, ActiveMouseButton.Middle, swallowInput: true);
        }
    }

    private void StartTracking(MouseHookEventArgs e, ActiveMouseButton button, bool swallowInput)
    {
        if (disposed || (isPaused && recordingRequestId is null))
        {
            return;
        }

        if (session.IsCompleting)
        {
            GestureStartDiagnostics.ReportButtonDownRecoveredWhileCompleting(
                ToPublicButton(button),
                session.Id);
            session.Cancel();
        }

        if (session.IsTracking)
        {
            var previousButton = activeMouseButton;
            CancelTracking();
            if (previousButton != button)
            {
                // The original Down was swallowed, so swallow its later Up if it arrives.
                suppressedReleaseButton = previousButton;
            }
        }

        var startTimestamp = Stopwatch.GetTimestamp();
        var gestureContext = recordingRequestId is null
            ? CreateGestureContext(e.Location)
            : GestureExecutionContext.Empty;
        var contextCompletedAt = Stopwatch.GetTimestamp();

        var isExcluded = recordingRequestId is null && IsGestureExcluded(gestureContext);
        var exclusionCompletedAt = Stopwatch.GetTimestamp();
        var fullscreenChecked = recordingRequestId is null && !isExcluded;
        var isFullscreen = fullscreenChecked && IsDisabledInFullscreen();
        var fullscreenCompletedAt = Stopwatch.GetTimestamp();
        if (recordingRequestId is null && (isExcluded || isFullscreen))
        {
            GestureStartDiagnostics.ReportButtonDown(
                startTimestamp,
                contextCompletedAt,
                exclusionCompletedAt,
                fullscreenCompletedAt,
                ToPublicButton(button),
                gestureContext.TargetWindowMode,
                gestureContext.StartWindow,
                gestureContext.ScopeContext,
                isExcluded,
                isFullscreen,
                fullscreenChecked);
            return;
        }

        if (recordingRequestId is null)
        {
            GestureStartDiagnostics.ReportButtonDown(
                startTimestamp,
                contextCompletedAt,
                exclusionCompletedAt,
                fullscreenCompletedAt,
                ToPublicButton(button),
                gestureContext.TargetWindowMode,
                gestureContext.StartWindow,
                gestureContext.ScopeContext,
                isExcluded,
                isFullscreen,
                fullscreenChecked);
        }

        e.Handled = swallowInput;
        if (suppressedReleaseButton == button)
        {
            suppressedReleaseButton = ActiveMouseButton.None;
        }

        activeMouseButton = button;
        session.Start(Interlocked.Increment(ref nextSessionId), e.Location);
        activeGestureContext = gestureContext;
        RaisePreviewCleared(session.Id);
        RaiseProgress(session.Id, session.Snapshot(), [], true, ToPublicButton(button), force: true);
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        lock (stateLock)
        {
            if (disposed || !session.IsTracking)
            {
                return;
            }

            if (recordingRequestId is null && IsDisabledInFullscreen())
            {
                CancelTracking();
                return;
            }

            var lastPoint = session.LastPoint;
            if (Distance(lastPoint, e.Location) < GestureRuntimeDefaults.MinimumPointDistance)
            {
                return;
            }

            var moveReceivedAt = Stopwatch.GetTimestamp();
            session.Append(e.Location);
            PublishProgress(moveReceivedAt);
        }
    }

    private void OnRightButtonUp(object? sender, MouseHookEventArgs e)
    {
        lock (stateLock)
        {
            if (disposed || !session.IsTracking || activeMouseButton != ActiveMouseButton.Right)
            {
                SuppressAbandonedButtonUp(e, ActiveMouseButton.Right);
                return;
            }

            e.Handled = true;
            FinishTracking(e.Location, ActiveMouseButton.Right);
        }
    }

    private void OnMiddleButtonUp(object? sender, MouseHookEventArgs e)
    {
        lock (stateLock)
        {
            if (disposed || !session.IsTracking || activeMouseButton != ActiveMouseButton.Middle)
            {
                SuppressAbandonedButtonUp(e, ActiveMouseButton.Middle);
                return;
            }

            e.Handled = true;
            FinishTracking(e.Location, ActiveMouseButton.Middle);
        }
    }

    private void CancelTracking()
    {
        if (!session.IsTracking && !session.IsCompleting)
        {
            return;
        }

        var button = activeMouseButton;
        var sessionId = session.Id;
        var path = session.Snapshot();

        activeMouseButton = ActiveMouseButton.None;
        activeGestureContext = GestureExecutionContext.Empty;
        ClearPreviewMatch(sessionId);
        session.Cancel();
        RaiseProgress(sessionId, path, [], false, ToPublicButton(button), force: true);
    }

    private void FinishTracking(Point location, ActiveMouseButton button)
    {
        var completionStartedAt = Stopwatch.GetTimestamp();
        var sessionId = session.Id;
        var recordingId = recordingRequestId;
        var gestureContext = activeGestureContext;
        recordingRequestId = null;
        session.BeginCompleting();
        activeMouseButton = ActiveMouseButton.None;
        activeGestureContext = GestureExecutionContext.Empty;
        session.Append(location);
        var path = session.Snapshot();

        if (recordingId is not null)
        {
            var recordingPattern = recognizer.Recognize(session.Points);
            RaiseProgress(sessionId, path, recordingPattern, false, ToPublicButton(button), force: true);
            ClearPreviewMatch(sessionId);
            CompleteSession(sessionId);
            Post(() =>
            {
                if (disposed)
                {
                    return;
                }

                GestureRecordingCompleted?.Invoke(
                    this,
                    new GestureRecordingCompletedEventArgs(sessionId, recordingId, path, recordingPattern, ToPublicButton(button)));
            });
            return;
        }

        if (session.Points.Count < 2 || PathLength(session.Points) < minimumGestureDistance)
        {
            RaiseProgress(sessionId, path, [], false, ToPublicButton(button), force: true);
            CompleteSession(sessionId);
            if (button == ActiveMouseButton.Right)
            {
                Post(() =>
                {
                    if (disposed)
                    {
                        return;
                    }

                    MouseInput.ReplayRightClick();
                });
            }
            else if (button == ActiveMouseButton.Middle)
            {
                Post(() =>
                {
                    if (disposed)
                    {
                        return;
                    }

                    MouseInput.ReplayMiddleClick();
                });
            }

            return;
        }

        var publicButton = ToPublicButton(button);
        var pattern = recognizer.Recognize(session.Points);
        var rule = matcher.Match(pattern, gestureContext.ScopeContext, publicButton);
        if (rule is null)
        {
            RaiseProgress(sessionId, path, pattern, false, publicButton, force: true);
            CompleteSession(sessionId);
            return;
        }

        var postedAt = Stopwatch.GetTimestamp();
        CompleteSession(sessionId);
        Post(() =>
        {
            var callbackStartedAt = Stopwatch.GetTimestamp();
            var feedbackCompletedAt = callbackStartedAt;
            var targetCheckedAt = feedbackCompletedAt;
            var foregroundActivatedAt = targetCheckedAt;
            var outcome = "completed";
            if (disposed)
            {
                return;
            }

            try
            {
                GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(sessionId, path, pattern, rule.ActionName));
                feedbackCompletedAt = Stopwatch.GetTimestamp();
                var targetWindow = gestureContext.UsesStartWindow
                    ? gestureContext.StartWindow
                    : GetForegroundWindow();
                var isDesktopWindowControlTarget = rule.Action is WindowControlAction &&
                                                   targetWindow != IntPtr.Zero &&
                                                   DesktopWindowClassifier.IsDesktopSurface(targetWindow);
                targetCheckedAt = Stopwatch.GetTimestamp();
                if (isDesktopWindowControlTarget)
                {
                    outcome = "desktop-window-control-ignored";
                    return;
                }

                if (RunsInBackground(rule))
                {
                    if (gestureContext.UsesStartWindow && gestureContext.StartWindow == IntPtr.Zero)
                    {
                        outcome = "start-window-unresolved";
                        return;
                    }

                    DispatchAction(new PendingAction(
                        sessionId,
                        path,
                        pattern,
                        rule,
                        targetWindow,
                        gestureContext.UsesStartWindow));
                    foregroundActivatedAt = Stopwatch.GetTimestamp();
                    outcome = "dispatched";
                }
                else
                {
                    if (gestureContext.UsesStartWindow)
                    {
                        if (gestureContext.StartWindow == IntPtr.Zero)
                        {
                            outcome = "start-window-unresolved";
                            return;
                        }

                        SetForegroundWindow(gestureContext.StartWindow);
                    }

                    foregroundActivatedAt = Stopwatch.GetTimestamp();
                    actionExecutor.Execute(rule, targetWindow);
                }
            }
            catch (Exception exception)
            {
                outcome = $"failed:{exception.GetType().Name}";
                AppLogger.Error("GestureService", "action-failed", $"Gesture action failed. Session: {sessionId}; action: {rule.ActionName}; pattern: {string.Join(",", pattern)}.", exception);
                GestureActionFailed?.Invoke(this, new GestureActionFailedEventArgs(sessionId, path, pattern, rule.ActionName, exception));
            }
            finally
            {
                GestureStartDiagnostics.ReportMatchedCompletion(
                    completionStartedAt,
                    postedAt,
                    callbackStartedAt,
                    feedbackCompletedAt,
                    targetCheckedAt,
                    foregroundActivatedAt,
                    Stopwatch.GetTimestamp(),
                    publicButton,
                    rule.ActionName,
                    outcome);
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

    private void DispatchAction(PendingAction action)
    {
        pendingActions.Enqueue(action);
        ScheduleActionWorker();
    }

    private void ScheduleActionWorker()
    {
        if (Interlocked.Exchange(ref actionWorkerScheduled, 1) == 0)
        {
            ThreadPool.UnsafeQueueUserWorkItem<object?>(static state =>
            {
                ((GestureService)state!).ProcessPendingActions();
            }, this, preferLocal: false);
        }
    }

    private void ProcessPendingActions()
    {
        try
        {
            while (pendingActions.TryDequeue(out var pendingAction))
            {
                if (disposed)
                {
                    continue;
                }

                var startedAt = Stopwatch.GetTimestamp();
                var outcome = "completed";
                try
                {
                    if (pendingAction.ActivateTargetWindow)
                    {
                        SetForegroundWindow(pendingAction.TargetWindow);
                    }

                    actionExecutor.Execute(pendingAction.Rule, pendingAction.TargetWindow);
                }
                catch (Exception exception)
                {
                    outcome = $"failed:{exception.GetType().Name}";
                    AppLogger.Error("GestureService", "background-action-failed", $"Background gesture action failed. Session: {pendingAction.SessionId}; action: {pendingAction.Rule.ActionName}; pattern: {string.Join(",", pendingAction.Pattern)}.", exception);
                    Post(() =>
                    {
                        if (!disposed)
                        {
                            GestureActionFailed?.Invoke(
                                this,
                                new GestureActionFailedEventArgs(
                                    pendingAction.SessionId,
                                    pendingAction.Path,
                                    pendingAction.Pattern,
                                    pendingAction.Rule.ActionName,
                                    exception));
                        }
                    });
                }
                finally
                {
                    GestureStartDiagnostics.ReportActionExecution(
                        pendingAction.SessionId,
                        startedAt,
                        Stopwatch.GetTimestamp(),
                        pendingAction.Rule.ActionName,
                        outcome);
                }
            }
        }
        finally
        {
            Volatile.Write(ref actionWorkerScheduled, 0);
            if (!pendingActions.IsEmpty)
            {
                ScheduleActionWorker();
            }
        }
    }

    private static bool RunsInBackground(GestureRule rule)
    {
        return rule.Action is HotkeyAction or WindowControlAction;
    }

    internal bool HasNewerSession(long sessionId)
    {
        return Volatile.Read(ref nextSessionId) > sessionId;
    }

    private void CompleteSession(long sessionId)
    {
        session.Complete(sessionId);
    }

    private void PublishProgress(long? sourceTimestamp = null)
    {
        if (recordingRequestId is not null)
        {
            RaiseProgress(session.Id, session.Snapshot(), [], true, ToPublicButton(activeMouseButton), sourceTimestamp: sourceTimestamp);
            return;
        }

        var pattern = recognizer.Recognize(session.Points);
        var path = session.Snapshot();
        RaiseProgress(session.Id, path, pattern, true, ToPublicButton(activeMouseButton), sourceTimestamp: sourceTimestamp);

        RaisePreviewMatch(session.Id, path, pattern, activeGestureContext.ScopeContext, ToPublicButton(activeMouseButton));
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

        if (rule.ActionName == session.PreviewActionName)
        {
            return;
        }

        session.PreviewActionName = rule.ActionName;
        session.PreviewSessionId = sessionId;
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
        if (session.PreviewActionName is null)
        {
            return;
        }

        var previewSessionId = session.ClearPreview(sessionId);
        RaisePreviewCleared(previewSessionId);
    }

    private void RaisePreviewCleared(long sessionId)
    {
        Post(() =>
        {
            if (!disposed)
            {
                GesturePreviewCleared?.Invoke(this, new GesturePreviewClearedEventArgs(sessionId));
            }
        });
    }

    private void RaiseProgress(
        long sessionId,
        IReadOnlyList<Point> path,
        IReadOnlyList<GestureDirection> pattern,
        bool isCurrentlyTracking,
        GestureMouseButton button,
        bool force = false,
        long? sourceTimestamp = null)
    {
        if (!session.ShouldPublishProgress(pattern, path, force))
        {
            return;
        }

        var progress = new GestureProgressEventArgs(
            sessionId,
            path,
            pattern,
            isCurrentlyTracking,
            button,
            sourceTimestamp);
        if (isCurrentlyTracking && !force)
        {
            Volatile.Write(ref pendingTrackingProgress, progress);
            if (Interlocked.Exchange(ref trackingProgressPostScheduled, 1) == 0)
            {
                Post(DispatchTrackingProgress);
            }

            return;
        }

        Post(() =>
        {
            if (!disposed)
            {
                GestureProgressChanged?.Invoke(this, progress);
            }
        });
    }

    private void DispatchTrackingProgress()
    {
        var progress = Interlocked.Exchange(ref pendingTrackingProgress, null);
        Volatile.Write(ref trackingProgressPostScheduled, 0);
        if (!disposed && progress is not null)
        {
            GestureProgressChanged?.Invoke(this, progress);
        }

        if (Volatile.Read(ref pendingTrackingProgress) is not null &&
            Interlocked.CompareExchange(ref trackingProgressPostScheduled, 1, 0) == 0)
        {
            Post(DispatchTrackingProgress);
        }
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
