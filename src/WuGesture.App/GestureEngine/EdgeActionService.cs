using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

public sealed class EdgeActionService : IDisposable
{
    private readonly MouseHook mouseHook = new();
    private readonly ActionExecutor actionExecutor = new();
    private readonly ApplicationExclusionMatcher exclusionMatcher = new();
    private readonly System.Windows.Forms.Timer mousePollTimer = new() { Interval = EdgeActionRuntimeDefaults.MousePollIntervalMs };
    private IReadOnlyList<EdgeActionConfig> actions;
    private SynchronizationContext? synchronizationContext;
    private EdgeLocation activeCorner = EdgeLocation.None;
    private EdgeLocation activeFrictionEdge = EdgeLocation.None;
    private FrictionAxisDirection lastFrictionDirection = FrictionAxisDirection.None;
    private int frictionCount;
    private int frictionPeakPosition;
    private DateTime lastFrictionMoveTime;
    private bool frictionTriggered;
    private bool started;
    private bool paused;
    private bool disposed;

    public EdgeActionService(IEnumerable<EdgeActionConfig> actions)
    {
        this.actions = NormalizeActions(actions);
    }

    public event EventHandler<EdgeActionFailedEventArgs>? EdgeActionFailed;

    public void UpdateActions(IEnumerable<EdgeActionConfig> nextActions)
    {
        actions = NormalizeActions(nextActions);
        ResetFriction();
    }

    public void UpdateExcludedApplications(IEnumerable<ExcludedApplicationConfig>? applications)
    {
        exclusionMatcher.Update(applications);
        if (exclusionMatcher.IsEdgeActionExcluded())
        {
            activeCorner = EdgeLocation.None;
            activeFrictionEdge = EdgeLocation.None;
            ResetFriction();
        }
    }

    public void SetPaused(bool isPaused)
    {
        paused = isPaused;
        if (paused)
        {
            activeCorner = EdgeLocation.None;
            activeFrictionEdge = EdgeLocation.None;
            ResetFriction();
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
        HandleMouseLocation(Cursor.Position);
    }

    private void HandleMouseLocation(Point location)
    {
        if (disposed || paused || exclusionMatcher.IsEdgeActionExcluded())
        {
            return;
        }

        if (IsAnyMouseButtonPressed())
        {
            activeCorner = EdgeLocation.None;
            activeFrictionEdge = EdgeLocation.None;
            ResetFriction();
            return;
        }

        var corner = GetCorner(location);
        var frictionEdge = GetFrictionEdge(location);
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
            ResetFriction();
            return;
        }

        if (activeFrictionEdge != frictionEdge)
        {
            BeginFriction(frictionEdge, location);
            return;
        }

        HandleFriction(frictionEdge, location);
    }

    private void HandleFriction(EdgeLocation edge, Point location)
    {
        if (frictionTriggered)
        {
            if (GetFrictionDistanceToEdge(edge, location) >= EdgeActionRuntimeDefaults.FrictionResetDistance ||
                DateTime.UtcNow - lastFrictionMoveTime > EdgeActionRuntimeDefaults.FrictionTriggerResetTimeout)
            {
                activeFrictionEdge = EdgeLocation.None;
                ResetFriction();
            }

            return;
        }

        var currentPosition = GetFrictionPosition(edge, location);
        var delta = currentPosition - frictionPeakPosition;
        var direction = delta > 0 ? FrictionAxisDirection.Positive : FrictionAxisDirection.Negative;
        if (lastFrictionDirection != FrictionAxisDirection.None && lastFrictionDirection == direction)
        {
            frictionPeakPosition = currentPosition;
            return;
        }

        if (Math.Abs(delta) < EdgeActionRuntimeDefaults.FrictionStepPixels)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (lastFrictionDirection != FrictionAxisDirection.None && now - lastFrictionMoveTime > EdgeActionRuntimeDefaults.FrictionMoveTimeout)
        {
            BeginFriction(edge, location);
            return;
        }

        lastFrictionMoveTime = now;
        frictionPeakPosition = currentPosition;
        lastFrictionDirection = direction;
        frictionCount++;

        foreach (var action in GetActions(GestureConfigContract.EdgeTriggerTypes.Friction, edge))
        {
            if (frictionCount >= Math.Max(1, action.FrictionCount))
            {
                frictionTriggered = true;
                Execute(action);
                return;
            }
        }
    }

    private void BeginFriction(EdgeLocation edge, Point location)
    {
        activeFrictionEdge = edge;
        ResetFriction();
        frictionPeakPosition = GetFrictionPosition(edge, location);
        lastFrictionMoveTime = DateTime.UtcNow;
    }

    private void OnMouseWheel(object? sender, MouseWheelHookEventArgs e)
    {
        if (disposed || paused || exclusionMatcher.IsEdgeActionExcluded())
        {
            return;
        }

        var edge = GetEdge(e.Location);
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

        e.Handled = true;
        Execute(matched);
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
        var locationName = ToConfigLocation(location);
        return actions.Where(action =>
            action.Enabled &&
            string.Equals(action.TriggerType, triggerType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action.Location, locationName, StringComparison.OrdinalIgnoreCase) &&
            GestureConfigMapper.ToAction(action.Action) is not null);
    }

    private void Execute(EdgeActionConfig config)
    {
        var action = GestureConfigMapper.ToAction(config.Action);
        if (action is null)
        {
            return;
        }

        Post(() =>
        {
            if (disposed)
            {
                return;
            }

            try
            {
                var actionName = GetActionName(config);
                actionExecutor.Execute(new GestureRule([], GestureConfigContract.Scopes.Global, actionName, action), IntPtr.Zero);
            }
            catch (Exception exception)
            {
                EdgeActionFailed?.Invoke(this, new EdgeActionFailedEventArgs(GetActionName(config), exception));
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

    private static EdgeLocation GetCorner(Point location)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var area = screen.Bounds;
            var left = location.X <= area.Left + EdgeActionRuntimeDefaults.CornerSize;
            var right = location.X >= area.Right - EdgeActionRuntimeDefaults.CornerSize;
            var top = location.Y <= area.Top + EdgeActionRuntimeDefaults.CornerSize;
            var bottom = location.Y >= area.Bottom - EdgeActionRuntimeDefaults.CornerSize;

            if (left && top)
            {
                return EdgeLocation.TopLeft;
            }

            if (right && top)
            {
                return EdgeLocation.TopRight;
            }

            if (left && bottom)
            {
                return EdgeLocation.BottomLeft;
            }

            if (right && bottom)
            {
                return EdgeLocation.BottomRight;
            }
        }

        return EdgeLocation.None;
    }

    private static EdgeLocation GetEdge(Point location)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var area = screen.Bounds;
            if (location.X < area.Left || location.X > area.Right || location.Y < area.Top || location.Y > area.Bottom)
            {
                continue;
            }

            if (location.X <= area.Left + EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Left;
            }

            if (location.X >= area.Right - EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Right;
            }

            if (location.Y <= area.Top + EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Top;
            }

            if (location.Y >= area.Bottom - EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Bottom;
            }
        }

        return EdgeLocation.None;
    }

    private static EdgeLocation GetFrictionEdge(Point location)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var area = screen.Bounds;
            if (location.X < area.Left || location.X > area.Right || location.Y < area.Top || location.Y > area.Bottom)
            {
                continue;
            }

            var x = location.X - area.Left;
            var y = location.Y - area.Top;
            var width = area.Width;
            var height = area.Height;

            if (x <= EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                y > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                y < height - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Left;
            }

            if (x >= width - EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                y > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                y < height - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Right;
            }

            if (y <= EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                x > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                x < width - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Top;
            }

            if (y >= height - EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                x > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                x < width - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Bottom;
            }
        }

        return EdgeLocation.None;
    }

    private static int GetFrictionPosition(EdgeLocation edge, Point location)
    {
        return edge is EdgeLocation.Left or EdgeLocation.Right
            ? location.Y
            : location.X;
    }

    private static int GetFrictionDistanceToEdge(EdgeLocation edge, Point location)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var area = screen.Bounds;
            if (location.X < area.Left || location.X > area.Right || location.Y < area.Top || location.Y > area.Bottom)
            {
                continue;
            }

            return edge switch
            {
                EdgeLocation.Left => location.X - area.Left,
                EdgeLocation.Right => area.Right - location.X,
                EdgeLocation.Top => location.Y - area.Top,
                EdgeLocation.Bottom => area.Bottom - location.Y,
                _ => int.MaxValue
            };
        }

        return int.MaxValue;
    }

    private static IReadOnlyList<EdgeActionConfig> NormalizeActions(IEnumerable<EdgeActionConfig>? source)
    {
        return (source ?? [])
            .Where(action => !string.IsNullOrWhiteSpace(action.TriggerType) && !string.IsNullOrWhiteSpace(action.Location))
            .Select(NormalizeAction)
            .ToArray();
    }

    private static EdgeActionConfig NormalizeAction(EdgeActionConfig action)
    {
        if (string.Equals(action.TriggerType, GestureConfigContract.EdgeTriggerTypes.Friction, StringComparison.OrdinalIgnoreCase))
        {
            action.Location = MigrateLegacyFrictionLocation(action.Location);
            if (!IsEdgeLocation(action.Location))
            {
                action.Location = GestureConfigContract.EdgeLocations.Left;
            }
        }
        else if (string.Equals(action.TriggerType, GestureConfigContract.EdgeTriggerTypes.Wheel, StringComparison.OrdinalIgnoreCase) &&
            !IsEdgeLocation(action.Location))
        {
            action.Location = GestureConfigContract.EdgeLocations.Left;
        }

        return action;
    }

    private static bool IsEdgeLocation(string location)
    {
        return location.Trim().ToLowerInvariant() is
            GestureConfigContract.EdgeLocations.Left or
            GestureConfigContract.EdgeLocations.Right or
            GestureConfigContract.EdgeLocations.Top or
            GestureConfigContract.EdgeLocations.Bottom;
    }

    private static string MigrateLegacyFrictionLocation(string location)
    {
        return location.Trim().ToLowerInvariant() switch
        {
            GestureConfigContract.EdgeLocations.TopLeft => GestureConfigContract.EdgeLocations.Left,
            GestureConfigContract.EdgeLocations.TopRight => GestureConfigContract.EdgeLocations.Top,
            GestureConfigContract.EdgeLocations.BottomLeft => GestureConfigContract.EdgeLocations.Bottom,
            GestureConfigContract.EdgeLocations.BottomRight => GestureConfigContract.EdgeLocations.Right,
            _ => location
        };
    }

    private static string ToConfigLocation(EdgeLocation location)
    {
        return location switch
        {
            EdgeLocation.TopLeft => GestureConfigContract.EdgeLocations.TopLeft,
            EdgeLocation.TopRight => GestureConfigContract.EdgeLocations.TopRight,
            EdgeLocation.BottomLeft => GestureConfigContract.EdgeLocations.BottomLeft,
            EdgeLocation.BottomRight => GestureConfigContract.EdgeLocations.BottomRight,
            EdgeLocation.Left => GestureConfigContract.EdgeLocations.Left,
            EdgeLocation.Right => GestureConfigContract.EdgeLocations.Right,
            EdgeLocation.Top => GestureConfigContract.EdgeLocations.Top,
            EdgeLocation.Bottom => GestureConfigContract.EdgeLocations.Bottom,
            _ => ""
        };
    }

    private static string GetActionName(EdgeActionConfig config)
    {
        var triggerLabel = (config.TriggerType ?? "").Trim().ToLowerInvariant() switch
        {
            GestureConfigContract.EdgeTriggerTypes.Corner => "触发角",
            GestureConfigContract.EdgeTriggerTypes.Friction => "摩擦边",
            GestureConfigContract.EdgeTriggerTypes.Wheel => "边缘滚动",
            _ => "边缘操作"
        };
        var locationLabel = (config.Location ?? "").Trim().ToLowerInvariant() switch
        {
            GestureConfigContract.EdgeLocations.TopLeft => "左上角",
            GestureConfigContract.EdgeLocations.TopRight => "右上角",
            GestureConfigContract.EdgeLocations.BottomLeft => "左下角",
            GestureConfigContract.EdgeLocations.BottomRight => "右下角",
            GestureConfigContract.EdgeLocations.Left => "左边",
            GestureConfigContract.EdgeLocations.Right => "右边",
            GestureConfigContract.EdgeLocations.Top => "上边",
            GestureConfigContract.EdgeLocations.Bottom => "下边",
            _ => ""
        };
        var wheelLabel = string.Equals(config.WheelDirection, GestureConfigContract.WheelDirections.Down, StringComparison.OrdinalIgnoreCase)
            ? "滚轮下"
            : string.Equals(config.WheelDirection, GestureConfigContract.WheelDirections.Up, StringComparison.OrdinalIgnoreCase)
                ? "滚轮上"
                : "";

        return string.Join(' ', new[] { triggerLabel, locationLabel, wheelLabel }.Where(static part => part.Length > 0));
    }

    private void ResetFriction()
    {
        lastFrictionDirection = FrictionAxisDirection.None;
        frictionCount = 0;
        frictionPeakPosition = 0;
        lastFrictionMoveTime = DateTime.MinValue;
        frictionTriggered = false;
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

    private enum EdgeLocation
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Left,
        Right,
        Top,
        Bottom
    }

    private enum FrictionAxisDirection
    {
        None,
        Positive,
        Negative
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
