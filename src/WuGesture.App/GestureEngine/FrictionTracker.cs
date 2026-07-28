using System.Drawing;

namespace WuGesture.App.GestureEngine;

internal sealed class FrictionTracker
{
    private FrictionAxisDirection lastDirection;
    private int peakPosition;
    private DateTime lastMoveTime;

    public int Count { get; private set; }

    public bool IsTriggered { get; private set; }

    public void Begin(EdgeLocation edge, Point location, DateTime now)
    {
        Count = 0;
        IsTriggered = false;
        lastDirection = FrictionAxisDirection.None;
        peakPosition = GetPosition(edge, location);
        lastMoveTime = now;
    }

    public bool TryRegisterDirectionChange(EdgeLocation edge, Point location, DateTime now)
    {
        var currentPosition = GetPosition(edge, location);
        var delta = currentPosition - peakPosition;
        var direction = delta > 0 ? FrictionAxisDirection.Positive : FrictionAxisDirection.Negative;
        if (lastDirection != FrictionAxisDirection.None && lastDirection == direction)
        {
            peakPosition = currentPosition;
            return false;
        }

        if (Math.Abs(delta) < EdgeActionRuntimeDefaults.FrictionStepPixels)
        {
            return false;
        }

        if (lastDirection != FrictionAxisDirection.None && now - lastMoveTime > EdgeActionRuntimeDefaults.FrictionMoveTimeout)
        {
            Begin(edge, location, now);
            return false;
        }

        lastMoveTime = now;
        peakPosition = currentPosition;
        lastDirection = direction;
        Count++;
        return true;
    }

    public bool ShouldResetTriggered(int distanceToEdge, DateTime now)
    {
        return distanceToEdge >= EdgeActionRuntimeDefaults.FrictionResetDistance ||
            now - lastMoveTime > EdgeActionRuntimeDefaults.FrictionTriggerResetTimeout;
    }

    public void MarkTriggered()
    {
        IsTriggered = true;
    }

    public void Reset()
    {
        Count = 0;
        IsTriggered = false;
        lastDirection = FrictionAxisDirection.None;
        peakPosition = 0;
        lastMoveTime = DateTime.MinValue;
    }

    private static int GetPosition(EdgeLocation edge, Point location)
    {
        return edge is EdgeLocation.Left or EdgeLocation.Right ? location.Y : location.X;
    }

    private enum FrictionAxisDirection
    {
        None,
        Positive,
        Negative
    }
}
