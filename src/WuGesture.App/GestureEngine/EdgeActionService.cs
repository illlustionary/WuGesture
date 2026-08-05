using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

public sealed class EdgeActionService : IDisposable
{
    private readonly object stateLock = new();
    private readonly MouseHook mouseHook = new();
    private readonly ActionExecutor actionExecutor = new();
    private readonly ApplicationExclusionMatcher exclusionMatcher = new();
    private readonly System.Windows.Forms.Timer mousePollTimer = new() { Interval = EdgeActionRuntimeDefaults.MousePollIntervalMs };
    private readonly object wheelQueueLock = new();
    private readonly LinkedList<WheelActionWorkItem> wheelActionQueue = new();
    private IReadOnlyList<EdgeActionConfig> actions;
    private SynchronizationContext? synchronizationContext;
    private EdgeLocation activeCorner = EdgeLocation.None;
    private EdgeLocation activeFrictionEdge = EdgeLocation.None;
    private readonly FrictionTracker frictionTracker = new();
    private bool started;
    private volatile bool paused;
    private bool disableEdgeActionsInFullscreen;
    private volatile bool wheelActionsBlocked = true;
    private bool wheelWorkerRunning;
    private volatile bool disposed;

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
        mousePollTimer.Tick += OnMousePollTimerTick;
        mousePollTimer.Start();
        mouseHook.Start();
        started = true;
    }

    public void Stop()
    {
        if (!started)
        {
            return;
        }

        mousePollTimer.Stop();
        mousePollTimer.Tick -= OnMousePollTimerTick;
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
        mousePollTimer.Dispose();
    }

    private void OnMousePollTimerTick(object? sender, EventArgs e)
    {
        lock (stateLock)
        {
            HandleMouseLocation(Cursor.Position);
        }
    }

    private void HandleMouseLocation(Point location)
    {
        if (disposed || paused)
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

    private static bool IsAnyMouseButtonPressed()
    {
        return IsKeyPressed(Keys.LButton) || IsKeyPressed(Keys.RButton) || IsKeyPressed(Keys.MButton);
    }

    private static bool IsKeyPressed(Keys key)
    {
        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

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
