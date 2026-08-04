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

    private readonly MouseHook mouseHook = new();
    private readonly GestureRecognizer recognizer = new();
    private GestureMatcher matcher;
    private readonly IGestureScopeContextProvider scopeContextProvider;
    private readonly ActionExecutor actionExecutor = new();
    private readonly ApplicationExclusionMatcher exclusionMatcher = new();
    private readonly GestureSession session = new();
    private readonly GestureParserWorker parserWorker = new();
    private readonly GestureInputCapture inputCapture;
    private readonly ConcurrentQueue<PendingAction> pendingActions = new();
    private GestureProgressEventArgs? pendingTrackingProgress;
    private ApplicationExclusionMatcher captureExclusionMatcher = new();
    private int minimumGestureDistance = GestureRuntimeDefaults.MinimumGestureDistance;
    private int actionWorkerScheduled;
    private int trackingProgressPostScheduled;
    private string? recordingRequestId;
    private string targetWindowMode = GestureConfigContract.WindowTargetModes.StartWindow;
    private string captureTargetWindowMode = GestureConfigContract.WindowTargetModes.StartWindow;
    private GestureExecutionContext activeGestureContext = GestureExecutionContext.Empty;
    private SynchronizationContext? synchronizationContext;
    private bool isPaused;
    private bool disableGesturesInFullscreen;
    private ActiveMouseButton activeMouseButton = ActiveMouseButton.None;
    private bool captureDisableGesturesInFullscreen;
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
        inputCapture = new GestureInputCapture(CanCaptureNormalGesture);
        inputCapture.GestureStarted += OnGestureStarted;
        inputCapture.GestureMoved += OnGestureMoved;
        inputCapture.GestureEnded += OnGestureEnded;
    }

    public void UpdateMatcher(GestureMatcher newMatcher)
    {
        EnqueueParserAction(service => service.matcher = newMatcher);
    }

    private sealed record GestureStartInput(
        Point Location,
        ActiveMouseButton Button,
        long Timestamp,
        string? RecordingRequestId);

    public void ApplyGestureSensitivity(GestureSensitivityUiSettings settings)
    {
        var percent = settings.Percent;
        EnqueueParserAction(service =>
        {
            service.recognizer.ApplySensitivity(percent);
            service.minimumGestureDistance = service.recognizer.MinimumGestureDistance;
        });
    }

    public void UpdateExcludedApplications(IEnumerable<ExcludedApplicationConfig>? applications)
    {
        var snapshot = (applications ?? []).ToArray();
        Volatile.Write(ref captureExclusionMatcher, new ApplicationExclusionMatcher(snapshot));
        EnqueueParserAction(service =>
        {
            service.exclusionMatcher.Update(snapshot);
            if (service.session.IsTracking && service.recordingRequestId is null && service.IsGestureExcluded(service.activeGestureContext))
            {
                service.CancelTracking();
            }
        });
    }

    public void UpdateFullscreenBehavior(bool disableGestures)
    {
        Volatile.Write(ref captureDisableGesturesInFullscreen, disableGestures);
        EnqueueParserAction(service =>
        {
            service.disableGesturesInFullscreen = disableGestures;
            if (service.disableGesturesInFullscreen && ForegroundWindowFullscreenDetector.IsFullscreenForegroundWindow())
            {
                service.CancelTracking();
            }
        });
    }

    public void UpdateTargetWindowMode(string? mode)
    {
        var normalizedMode = NormalizeTargetWindowMode(mode);
        Volatile.Write(ref captureTargetWindowMode, normalizedMode);
        EnqueueParserAction(service => service.targetWindowMode = normalizedMode);
    }

    public void SetPaused(bool paused)
    {
        if (disposed)
        {
            return;
        }

        inputCapture.SetPaused(paused);
        if (!paused)
        {
            StopRecording();
        }

        EnqueueParserAction(service =>
        {
            service.isPaused = paused;
            if (paused)
            {
                service.CancelTracking();
            }
        });
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

        inputCapture.SetRecordingRequest(trimmed);
        EnqueueParserAction(service =>
        {
            if (service.recordingRequestId == trimmed)
            {
                return;
            }

            service.CancelTracking();
            service.recordingRequestId = trimmed;
            service.ClearPreviewMatch();
        });
    }

    public void StopRecording()
    {
        inputCapture.SetRecordingRequest(null);
        EnqueueParserAction(service =>
        {
            if (service.recordingRequestId is null)
            {
                return;
            }

            service.recordingRequestId = null;
            if (service.session.IsTracking)
            {
                service.CancelTracking();
                return;
            }

            service.ClearPreviewMatch();
        });
    }

    public void Start()
    {
        if (disposed || started)
        {
            return;
        }

        synchronizationContext = SynchronizationContext.Current;
        parserWorker.Start();
        mouseHook.RightButtonDown += OnRightButtonDown;
        mouseHook.MiddleButtonDown += OnMiddleButtonDown;
        mouseHook.MouseMove += OnMouseMove;
        mouseHook.RightButtonUp += OnRightButtonUp;
        mouseHook.MiddleButtonUp += OnMiddleButtonUp;
        try
        {
            mouseHook.Start();
        }
        catch
        {
            mouseHook.RightButtonDown -= OnRightButtonDown;
            mouseHook.MiddleButtonDown -= OnMiddleButtonDown;
            mouseHook.MouseMove -= OnMouseMove;
            mouseHook.RightButtonUp -= OnRightButtonUp;
            mouseHook.MiddleButtonUp -= OnMiddleButtonUp;
            parserWorker.Dispose();
            throw;
        }
        GestureStartDiagnostics.ReportServiceStarted();
        started = true;
    }

    public void Stop()
    {
        if (!started)
        {
            return;
        }

        mouseHook.RightButtonDown -= OnRightButtonDown;
        mouseHook.MiddleButtonDown -= OnMiddleButtonDown;
        mouseHook.MouseMove -= OnMouseMove;
        mouseHook.RightButtonUp -= OnRightButtonUp;
        mouseHook.MiddleButtonUp -= OnMiddleButtonUp;
        mouseHook.Dispose();
        parserWorker.Dispose();
        started = false;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        inputCapture.Dispose();
        Stop();
    }

    private void OnRightButtonDown(object? sender, MouseHookEventArgs e)
    {
        inputCapture.HandleButtonDown(e, GestureInputButton.Right);
    }

    private void OnMiddleButtonDown(object? sender, MouseHookEventArgs e)
    {
        inputCapture.HandleButtonDown(e, GestureInputButton.Middle);
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        inputCapture.HandleMove(e);
    }

    private void OnRightButtonUp(object? sender, MouseHookEventArgs e)
    {
        inputCapture.HandleButtonUp(e, GestureInputButton.Right);
    }

    private void OnMiddleButtonUp(object? sender, MouseHookEventArgs e)
    {
        inputCapture.HandleButtonUp(e, GestureInputButton.Middle);
    }

    private void OnGestureStarted(Point location, GestureInputButton button, long timestamp, string? recordingId)
    {
        var start = new GestureStartInput(location, ToActiveMouseButton(button), timestamp, recordingId);
        parserWorker.Enqueue(() => StartTracking(start));
    }

    private void OnGestureMoved(Point location, long timestamp)
    {
        parserWorker.EnqueueLatestMove(() => ProcessMove(location, timestamp));
    }

    private void OnGestureEnded(Point location, GestureInputButton button)
    {
        var activeButton = ToActiveMouseButton(button);
        parserWorker.EnqueueAfterLatestMove(() => ProcessEnd(location, activeButton));
    }

    private void StartTracking(GestureStartInput item)
    {
        var button = item.Button;
        var recordingId = item.RecordingRequestId;
        if (session.IsCompleting)
        {
            GestureStartDiagnostics.ReportButtonDownRecoveredWhileCompleting(
                ToPublicButton(button),
                session.Id);
            session.Cancel();
        }

        if (session.IsTracking)
        {
            CancelTracking();
        }

        if (recordingId is not null)
        {
            recordingRequestId = recordingId;
        }

        var startTimestamp = item.Timestamp;
        var gestureContext = recordingId is null
            ? CreateGestureContext(item.Location)
            : GestureExecutionContext.Empty;
        var contextCompletedAt = Stopwatch.GetTimestamp();

        var isExcluded = recordingId is null && IsGestureExcluded(gestureContext);
        var exclusionCompletedAt = Stopwatch.GetTimestamp();
        var fullscreenChecked = recordingId is null && !isExcluded;
        var isFullscreen = fullscreenChecked && IsDisabledInFullscreen();
        var fullscreenCompletedAt = Stopwatch.GetTimestamp();
        if (recordingId is null && (isExcluded || isFullscreen))
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

        if (recordingId is null)
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

        activeMouseButton = button;
        session.Start(Interlocked.Increment(ref nextSessionId), item.Location);
        activeGestureContext = gestureContext;
        RaisePreviewCleared(session.Id);
        RaiseProgress(session.Id, session.Snapshot(), [], true, ToPublicButton(button), force: true);
    }

    private void ProcessMove(Point location, long timestamp)
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

        if (Distance(session.LastPoint, location) < GestureRuntimeDefaults.MinimumPointDistance)
        {
            return;
        }

        session.Append(location);
        PublishProgress(timestamp);
    }

    private void ProcessEnd(Point location, ActiveMouseButton button)
    {
        if (!disposed && session.IsTracking && activeMouseButton == button)
        {
            FinishTracking(location, button);
        }
    }

    private void EnqueueParserAction(Action<GestureService> action)
    {
        if (!disposed)
        {
            parserWorker.Enqueue(() => action(this));
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

    private bool CanCaptureNormalGesture(Point location)
    {
        var exclusionMatcher = Volatile.Read(ref captureExclusionMatcher);
        var targetWindowMode = Volatile.Read(ref captureTargetWindowMode);
        var targetWindow = targetWindowMode == GestureConfigContract.WindowTargetModes.StartWindow
            ? ResolveStartTargetWindow(location)
            : IntPtr.Zero;
        if (targetWindowMode == GestureConfigContract.WindowTargetModes.StartWindow
            ? exclusionMatcher.IsGestureExcluded(targetWindow)
            : exclusionMatcher.IsGestureExcluded())
        {
            return false;
        }

        return !Volatile.Read(ref captureDisableGesturesInFullscreen) ||
               !ForegroundWindowFullscreenDetector.IsFullscreenForegroundWindow();
    }

    private bool IsDisabledInFullscreen()
    {
        return disableGesturesInFullscreen && ForegroundWindowFullscreenDetector.IsFullscreenForegroundWindow();
    }

    private static string NormalizeTargetWindowMode(string? mode)
    {
        return mode == GestureConfigContract.WindowTargetModes.CurrentWindow
            ? GestureConfigContract.WindowTargetModes.CurrentWindow
            : GestureConfigContract.WindowTargetModes.StartWindow;
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

    private static ActiveMouseButton ToActiveMouseButton(GestureInputButton button)
    {
        return button == GestureInputButton.Middle
            ? ActiveMouseButton.Middle
            : ActiveMouseButton.Right;
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
