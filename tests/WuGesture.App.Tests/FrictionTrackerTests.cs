using System.Drawing;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class FrictionTrackerTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TryRegisterDirectionChange_CountsOnlyReversals()
    {
        var tracker = new FrictionTracker();
        tracker.Begin(EdgeLocation.Left, new Point(0, 100), Start);

        Assert.True(tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 170), Start));
        Assert.Equal(1, tracker.Count);

        Assert.False(tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 240), Start));
        Assert.Equal(1, tracker.Count);

        Assert.True(tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 100), Start));
        Assert.Equal(2, tracker.Count);
    }

    [Fact]
    public void TryRegisterDirectionChange_IgnoresMovesBelowTheStepSize()
    {
        var tracker = new FrictionTracker();
        tracker.Begin(EdgeLocation.Left, new Point(0, 100), Start);

        Assert.False(tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 159), Start));
        Assert.Equal(0, tracker.Count);

        Assert.True(tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 160), Start));
        Assert.Equal(1, tracker.Count);
    }

    [Fact]
    public void TryRegisterDirectionChange_RestartsAfterTheMoveTimeout()
    {
        var tracker = new FrictionTracker();
        tracker.Begin(EdgeLocation.Left, new Point(0, 100), Start);
        Assert.True(tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 200), Start));
        Assert.Equal(1, tracker.Count);

        var lateMove = Start.AddMilliseconds(2000);

        Assert.False(tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 100), lateMove));
        Assert.Equal(0, tracker.Count);
    }

    [Fact]
    public void ShouldResetTriggered_UsesDistanceAndTimeouts()
    {
        var tracker = new FrictionTracker();
        tracker.Begin(EdgeLocation.Left, new Point(0, 100), Start);
        tracker.TryRegisterDirectionChange(EdgeLocation.Left, new Point(0, 200), Start);

        Assert.False(tracker.ShouldResetTriggered(49, Start.AddMilliseconds(1000)));
        Assert.True(tracker.ShouldResetTriggered(50, Start.AddMilliseconds(1000)));
        Assert.False(tracker.ShouldResetTriggered(10, Start.AddMilliseconds(1500)));
        Assert.True(tracker.ShouldResetTriggered(10, Start.AddMilliseconds(1501)));
    }

    [Fact]
    public void TryRegisterDirectionChange_UsesTheHorizontalAxisOnTopAndBottomEdges()
    {
        var tracker = new FrictionTracker();
        tracker.Begin(EdgeLocation.Top, new Point(100, 5), Start);

        Assert.True(tracker.TryRegisterDirectionChange(EdgeLocation.Top, new Point(170, 900), Start));
        Assert.Equal(1, tracker.Count);
    }

    [Fact]
    public void Reset_ClearsCountAndTriggerState()
    {
        var tracker = new FrictionTracker();
        tracker.Begin(EdgeLocation.Right, new Point(5, 100), Start);
        tracker.TryRegisterDirectionChange(EdgeLocation.Right, new Point(900, 170), Start);
        tracker.MarkTriggered();

        tracker.Reset();

        Assert.Equal(0, tracker.Count);
        Assert.False(tracker.IsTriggered);
    }
}
