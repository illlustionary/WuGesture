using System.Drawing;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class EdgeHitTesterTests
{
    [Fact]
    public void GetCorner_RecognizesCornersOnSecondaryScreens()
    {
        var screens = new[] { new Rectangle(-1920, 0, 1920, 1080) };

        var result = EdgeHitTester.GetCorner(new Point(-1910, 10), screens);

        Assert.Equal(EdgeLocation.TopLeft, result);
    }

    [Fact]
    public void GetEdge_UsesHorizontalEdgesBeforeVerticalEdges()
    {
        var screens = new[] { new Rectangle(0, 0, 1000, 800) };

        var result = EdgeHitTester.GetEdge(new Point(2, 2), screens);

        Assert.Equal(EdgeLocation.Left, result);
    }

    [Fact]
    public void GetFrictionEdge_ExcludesCornerAreas()
    {
        var screens = new[] { new Rectangle(0, 0, 1000, 800) };

        Assert.Equal(EdgeLocation.None, EdgeHitTester.GetFrictionEdge(new Point(5, 100), screens));
        Assert.Equal(EdgeLocation.Left, EdgeHitTester.GetFrictionEdge(new Point(5, 101), screens));
    }

    [Fact]
    public void GetFrictionDistanceToEdge_ReturnsTheDistanceWithinTheContainingScreen()
    {
        var screens = new[] { new Rectangle(-1920, 0, 1920, 1080) };

        var result = EdgeHitTester.GetFrictionDistanceToEdge(EdgeLocation.Left, new Point(-1910, 500), screens);

        Assert.Equal(10, result);
    }
}
