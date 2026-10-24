using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

public sealed class EdgeActionService : IDisposable
{
    private static readonly TimeSpan PollFailureLogInterval = TimeSpan.FromSeconds(10);

    private readonly object stateLock = new();
    private readonly MouseHook mouseHook = new();
    private readonly ActionExecutor actionExecutor = new();
    private readonly ApplicationExclusionMatcher exclusionMatcher = new();
    private readonly ManualResetEventSlim mousePollStop = new(initialState: true);
    private readonly object wheelQueueLock = new();
    private readonly LinkedList<WheelActionWorkItem> wheelActionQueue = new();
    private IReadOnlyList<EdgeActionConfig> actions;
    private SynchronizationContext? synchronizationContext;
    private EdgeLocation activeCorner = EdgeLocation.None;
    private EdgeLocation activeFrictionEdge = EdgeLocation.None;
    private readonly FrictionTracker frictionTracker = new();
    private Thread? mousePollThread;
    private long lastPollFailureLogTimestamp;
    private bool started;
    private volatile bool paused;
    private bool disableEdgeActionsInFullscreen;
    private volatile bool wheelActionsBlocked = true;
    private bool wheelWorkerRunning;
    private volatile bool disposed;
    private long suppressUntilTimestamp;

    public EdgeActionService(IEnumerable<EdgeActionConfig> actions)
    {
        this.actions = EdgeActionConfigNormalizer.Normalize(actions);
    }

    public event EventHandler<EdgeActionFailedEventArgs>? EdgeActionFailed;

    public void UpdateActions(IEnumerable<EdgeActionConfig> nextActions)
    {
        lock (stateLock)
        {
            actions = EdgeActionConfigNormalizer.Normalize(nextActions);
            frictionTracker.Reset();
        }
    }

    public void UpdateExcludedApplications(IEnumerable<ExcludedApplicationConfig>? applications)
    {
        lock (stateLock)
        {
            exclusionMatcher.Update(applications);
            wheelActionsBlocked = true;
            if (exclusionMatcher.IsEdgeActionExcluded())
            {
                activeCorner = EdgeLocation.None;
                activeFrictionEdge = EdgeLocation.None;
                frictionTracker.Reset();
            }
        }
    }

    public void UpdateFullscreenBehavior(bool disableEdgeActions)
    {
        lock (stateLock)
        {
            disableEdgeActionsInFullscreen = disableEdgeActions;
            wheelActionsBlocked = true;
            if (IsDisabledInFullscreen())
            {
                ResetActiveState();
            }
        }
    }

    public void SetPaused(bool isPaused)
    {
        lock (stateLock)
        {
            paused = isPaused;
            wheelActionsBlocked = isPaused;
            if (paused)
            {
                ResetActiveState();
            }
        }
    }

    public void Start()
    {
        if (disposed || started)
        {
            return;
        }

        synchronizationContext = SynchronizationContext.Current;
        mouseHook.MouseWheel += OnMouseWheel;
        StartMousePolling();
        try
        {
            mouseHook.Start();
        }
        catch
        {
            StopMousePolling();
            mouseHook.MouseWheel -= OnMouseWheel;
            throw;
        }

        started = true;
    }

    public void Stop()
    {
        if (!started)
        {
            return;
        }

        StopMousePolling();
        mouseHook.MouseWheel -= OnMouseWheel;
        mouseHook.Dispose();
        lock (wheelQueueLock)
        {
            wheelActionQueue.Clear();
        }
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
        mousePollStop.Dispose();
    }

    private void StartMousePolling()
    {
        mousePollStop.Reset();
        var thread = new Thread(MousePollLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
            Name = "WuGesture Edge Poll"
        };
        mousePollThread = thread;
        thread.Start();
    }

    private void StopMousePolling()
    {
        mousePollStop.Set();
        var thread = mousePollThread;
        mousePollThread = null;
        if (thread is not null && thread != Thread.CurrentThread)
        {
            thread.Join(TimeSpan.FromSeconds(1));
        }
    }

    /// <summary>
    /// Edge detection used to run on a WinForms timer on the UI thread, so any slow window
    /// or process query inside <see cref="HandleMouseLocation"/> froze the whole interface.
    /// It now runs on its own thread; actions still marshal back to the UI through <see cref="Post"/>.
    /// </summary>
    private void MousePollLoop()
    {
        while (true)
        {
            bool stopRequested;
            try
            {
                stopRequested = mousePollStop.Wait(EdgeActionRuntimeDefaults.MousePollIntervalMs);
            }
            catch (ObjectDisposedException)
            {
                // Dispose() only reaches this after the stop event was set and the join timed out.
                return;
            }

            if (stopRequested)
            {
                return;
            }

            try
            {
                lock (stateLock)
                {
                    if (disposed)
                    {
                        return;
                    }

                    if (!TryGetCursorPosition(out var cursor))
                    {
                        // GetCursorPos fails while another desktop is active (lock screen, UAC prompt,
                        // display wake-up). Cursor.Position would silently fall back to (0, 0), which is
                        // the top-left corner and would fire that edge action by itself.
                        ResetActiveState();
                        continue;
                    }

                    HandleMouseLocation(cursor);
                }
            }
            catch (Exception exception)
            {
                LogPollFailure(exception);
            }
        }
    }

    private void LogPollFailure(Exception exception)
    {
        var now = Stopwatch.GetTimestamp();
        var previous = Volatile.Read(ref lastPollFailureLogTimestamp);
        if (previous != 0 && Stopwatch.GetElapsedTime(previous, now) < PollFailureLogInterval)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref lastPollFailureLogTimestamp, now, previous) != previous)
        {
            return;
        }

        AppLogger.Error(
            "EdgeActionService",
            "mouse-poll-failed",
            "Polling the cursor position for edge actions failed.",
            exception);
    }

    private void HandleMouseLocation(Point location)
    {
        if (disposed || paused || IsSuppressed())
        {
            wheelActionsBlocked = true;
            ResetActiveState();
            return;
        }

        if (IsAnyMouseButtonPressed())
        {
            activeCorner = EdgeLocation.None;
            activeFrictionEdge = EdgeLocation.None;
            frictionTracker.Reset();
            return;
        }

        if (exclusionMatcher.IsEdgeActionExcluded() || IsDisabledInFullscreen())
        {
            wheelActionsBlocked = true;
            ResetActiveState();
            return;
        }

        wheelActionsBlocked = false;

        var corner = EdgeHitTester.GetCorner(location);
        var frictionEdge = EdgeHitTester.GetFrictionEdge(location);
        if (corner == EdgeLocation.None)
        {
            activeCorner = EdgeLocation.None;
        }

        if (corner != EdgeLocation.None && activeCorner != corner)
        {
            activeCorner = corner;
            ExecuteFirst(GestureConfigContract.EdgeTriggerTypes.Corner, corner);
        }

        if (frictionEdge == EdgeLocation.None)
        {
            activeFrictionEdge = EdgeLocation.None;
            frictionTracker.Reset();
            return;
        }

        if (activeFrictionEdge != frictionEdge)
        {
            activeFrictionEdge = frictionEdge;
            frictionTracker.Begin(frictionEdge, location, DateTime.UtcNow);
            return;
        }

        HandleFriction(frictionEdge, location);
    }

    private void HandleFriction(EdgeLocation edge, Point location)
    {
        var now = DateTime.UtcNow;
        if (frictionTracker.IsTriggered)
        {
            if (frictionTracker.ShouldResetTriggered(
                    EdgeHitTester.GetFrictionDistanceToEdge(edge, location),
                    now))
            {
                activeFrictionEdge = EdgeLocation.None;
                frictionTracker.Reset();
            }

            return;
        }

        if (!frictionTracker.TryRegisterDirectionChange(edge, location, now))
        {
            return;
        }

        foreach (var action in GetActions(GestureConfigContract.EdgeTriggerTypes.Friction, edge))
        {
            if (frictionTracker.Count >= Math.Max(1, action.FrictionCount))
            {
                frictionTracker.MarkTriggered();
                Execute(action);
                return;
            }
        }
    }

    private void OnMouseWheel(object? sender, MouseWheelHookEventArgs e)
    {
        if (disposed || paused || wheelActionsBlocked)
        {
            return;
        }

        var edge = EdgeHitTester.GetEdge(e.Location);
        if (edge == EdgeLocation.None)
        {
            return;
        }

        var wheelDirection = e.Delta > 0
            ? GestureConfigContract.WheelDirections.Up
            : GestureConfigContract.WheelDirections.Down;
        var matched = GetActions(GestureConfigContract.EdgeTriggerTypes.Wheel, edge)
            .FirstOrDefault(action => string.Equals(action.WheelDirection, wheelDirection, StringComparison.OrdinalIgnoreCase));
        if (matched is null)
        {
            return;
        }

        // The hook must decide whether to swallow the native wheel event before returning.
        e.Handled = true;
        EnqueueWheelAction(matched);
    }

    private void ExecuteFirst(string triggerType, EdgeLocation location)
    {
        var action = GetActions(triggerType, location).FirstOrDefault();
        if (action is not null)
        {
            Execute(action);
        }
    }

    private IEnumerable<EdgeActionConfig> GetActions(string triggerType, EdgeLocation location)
    {
        var locationName = EdgeActionLocationMapper.ToConfigLocation(location);
        var currentActions = Volatile.Read(ref actions);
        return currentActions.Where(action =>
            action.Enabled &&
            string.Equals(action.TriggerType, triggerType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action.Location, locationName, StringComparison.OrdinalIgnoreCase) &&
            GestureConfigMapper.ToAction(action.Action) is not null);
    }

    private void Execute(EdgeActionConfig config)
    {
        Post(() => ExecuteCore(config));
    }

    private void ExecuteCore(EdgeActionConfig config)
    {
        var action = GestureConfigMapper.ToAction(config.Action);
        if (action is null)
        {
            return;
        }

        try
        {
            if (disposed)
            {
                return;
            }

            var actionName = EdgeActionNameFormatter.Format(config);
            actionExecutor.Execute(
                new GestureRule([], GestureConfigContract.Scopes.Global, actionName, action),
                IntPtr.Zero,
                useCurrentWindowWhenTargetMissing: true);
        }
        catch (Exception exception)
        {
            var actionName = EdgeActionNameFormatter.Format(config);
            AppLogger.Error("EdgeActionService", "action-failed", $"Edge action failed: {actionName}.", exception);
            Post(() => EdgeActionFailed?.Invoke(this, new EdgeActionFailedEventArgs(actionName, exception)));
        }
    }

    private void EnqueueWheelAction(EdgeActionConfig config)
    {
        lock (wheelQueueLock)
        {
            if (wheelActionQueue.Last is { Value: var pending } && ReferenceEquals(pending.Config, config))
            {
                pending.Count++;
            }
            else
            {
                wheelActionQueue.AddLast(new WheelActionWorkItem(config));
            }

            if (wheelWorkerRunning)
            {
                return;
            }

            wheelWorkerRunning = true;
        }

        _ = Task.Run(ProcessWheelActionQueue);
    }

    private void ProcessWheelActionQueue()
    {
        while (true)
        {
            WheelActionWorkItem? workItem;
            lock (wheelQueueLock)
            {
                if (wheelActionQueue.Count == 0)
                {
                    wheelWorkerRunning = false;
                    return;
                }

                workItem = wheelActionQueue.First!.Value;
                wheelActionQueue.RemoveFirst();
            }

            if (disposed || paused || wheelActionsBlocked)
            {
                continue;
            }

            for (var index = 0; index < workItem.Count; index++)
            {
                if (disposed || paused || wheelActionsBlocked)
                {
                    break;
                }

                ExecuteCore(workItem.Config);
            }
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
        return disableEdgeActionsInFullscreen && ForegroundWindowFullscreenDetector.IsFullscreenForegroundWindow();
    }

    private void ResetActiveState()
    {
        activeCorner = EdgeLocation.None;
        activeFrictionEdge = EdgeLocation.None;
        frictionTracker.Reset();
    }

    /// <summary>
    /// Ignores cursor samples for a short while. Right after a power resume or display change the cursor
    /// position and the input desktop are still settling: the sample can report (0, 0), which is the
    /// top-left corner, and injected input is rejected while the desktop is still switching.
    /// </summary>
    public void SuppressFor(TimeSpan duration)
    {
        var until = Stopwatch.GetTimestamp() + (long)(duration.TotalSeconds * Stopwatch.Frequency);
        Interlocked.Exchange(ref suppressUntilTimestamp, until);
    }

    private bool IsSuppressed()
    {
        var until = Volatile.Read(ref suppressUntilTimestamp);
        return until != 0 && Stopwatch.GetTimestamp() < until;
    }

    private static bool IsAnyMouseButtonPressed()
    {
        return IsKeyPressed(Keys.LButton) || IsKeyPressed(Keys.RButton) || IsKeyPressed(Keys.MButton);
    }

    private static bool IsKeyPressed(Keys key)
    {
        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
    }

    private static bool TryGetCursorPosition(out Point position)
    {
        if (GetCursorPos(out var point))
        {
            position = new Point(point.X, point.Y);
            return true;
        }

        position = default;
        return false;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    private sealed class WheelActionWorkItem
    {
        public WheelActionWorkItem(EdgeActionConfig config)
        {
            Config = config;
        }

        public EdgeActionConfig Config { get; }

        public int Count { get; set; } = 1;
    }

}

public sealed class EdgeActionFailedEventArgs : EventArgs
{
    public EdgeActionFailedEventArgs(string actionName, Exception exception)
    {
        ActionName = actionName;
        Exception = exception;
    }

    public string ActionName { get; }

    public Exception Exception { get; }
}
