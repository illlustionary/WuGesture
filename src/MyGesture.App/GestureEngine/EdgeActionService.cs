using System.Drawing;
using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public sealed class EdgeActionService : IDisposable
{
    private const int EdgeThickness = 3;
    private const int CornerSize = 18;
    private const int FrictionStepPixels = 18;

    private readonly MouseHook mouseHook = new();
    private readonly ActionExecutor actionExecutor = new();
    private IReadOnlyList<EdgeActionConfig> actions;
    private SynchronizationContext? synchronizationContext;
    private EdgeLocation activeCorner = EdgeLocation.None;
    private Point lastLocation;
    private FrictionAxisDirection lastFrictionDirection = FrictionAxisDirection.None;
    private int frictionCount;
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

    public void SetPaused(bool isPaused)
    {
        paused = isPaused;
        if (paused)
        {
            activeCorner = EdgeLocation.None;
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
        mouseHook.MouseMove += OnMouseMove;
        mouseHook.MouseWheel += OnMouseWheel;
        mouseHook.Start();
        started = true;
    }

    public void Stop()
    {
        if (!started)
        {
            return;
        }

        mouseHook.MouseMove -= OnMouseMove;
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
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        if (disposed || paused)
        {
            return;
        }

        var corner = GetCorner(e.Location);
        if (corner == EdgeLocation.None)
        {
            activeCorner = EdgeLocation.None;
            ResetFriction();
            lastLocation = e.Location;
            return;
        }

        if (activeCorner != corner)
        {
            activeCorner = corner;
            ResetFriction();
            lastLocation = e.Location;
            ExecuteFirst("corner", corner);
            return;
        }

        HandleFriction(corner, e.Location);
    }

    private void HandleFriction(EdgeLocation corner, Point location)
    {
        var delta = GetFrictionDelta(corner, location, lastLocation);
        if (Math.Abs(delta) < FrictionStepPixels)
        {
            return;
        }

        lastLocation = location;
        var direction = delta > 0 ? FrictionAxisDirection.Positive : FrictionAxisDirection.Negative;
        if (lastFrictionDirection == FrictionAxisDirection.None)
        {
            lastFrictionDirection = direction;
            return;
        }

        if (lastFrictionDirection == direction)
        {
            return;
        }

        lastFrictionDirection = direction;
        frictionCount++;

        foreach (var action in GetActions("friction", corner))
        {
            if (frictionCount >= Math.Max(1, action.FrictionCount))
            {
                frictionCount = 0;
                Execute(action);
                return;
            }
        }
    }

    private void OnMouseWheel(object? sender, MouseWheelHookEventArgs e)
    {
        if (disposed || paused)
        {
            return;
        }

        var edge = GetEdge(e.Location);
        if (edge == EdgeLocation.None)
        {
            return;
        }

        var wheelDirection = e.Delta > 0 ? "up" : "down";
        var matched = GetActions("wheel", edge)
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
                actionExecutor.Execute(new GestureRule([], "global", config.ActionName, action), IntPtr.Zero);
            }
            catch (Exception exception)
            {
                EdgeActionFailed?.Invoke(this, new EdgeActionFailedEventArgs(config.ActionName, exception));
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

    private static int GetFrictionDelta(EdgeLocation corner, Point current, Point previous)
    {
        return corner is EdgeLocation.TopLeft or EdgeLocation.BottomLeft
            ? current.Y - previous.Y
            : current.X - previous.X;
    }

    private static EdgeLocation GetCorner(Point location)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var area = screen.Bounds;
            var left = location.X <= area.Left + CornerSize;
            var right = location.X >= area.Right - CornerSize;
            var top = location.Y <= area.Top + CornerSize;
            var bottom = location.Y >= area.Bottom - CornerSize;

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

            if (location.X <= area.Left + EdgeThickness)
            {
                return EdgeLocation.Left;
            }

            if (location.X >= area.Right - EdgeThickness)
            {
                return EdgeLocation.Right;
            }

            if (location.Y <= area.Top + EdgeThickness)
            {
                return EdgeLocation.Top;
            }

            if (location.Y >= area.Bottom - EdgeThickness)
            {
                return EdgeLocation.Bottom;
            }
        }

        return EdgeLocation.None;
    }

    private static IReadOnlyList<EdgeActionConfig> NormalizeActions(IEnumerable<EdgeActionConfig>? source)
    {
        return (source ?? [])
            .Where(action => !string.IsNullOrWhiteSpace(action.TriggerType) && !string.IsNullOrWhiteSpace(action.Location))
            .ToArray();
    }

    private static string ToConfigLocation(EdgeLocation location)
    {
        return location switch
        {
            EdgeLocation.TopLeft => "top-left",
            EdgeLocation.TopRight => "top-right",
            EdgeLocation.BottomLeft => "bottom-left",
            EdgeLocation.BottomRight => "bottom-right",
            EdgeLocation.Left => "left",
            EdgeLocation.Right => "right",
            EdgeLocation.Top => "top",
            EdgeLocation.Bottom => "bottom",
            _ => ""
        };
    }

    private void ResetFriction()
    {
        lastFrictionDirection = FrictionAxisDirection.None;
        frictionCount = 0;
    }

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
