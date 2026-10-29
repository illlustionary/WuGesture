using System.Drawing;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class FullscreenWindowHeuristicsTests
{
    [Fact]
    public void CoversMonitor_AcceptsExactAndOversizedBounds()
    {
        var monitor = new Rectangle(0, 0, 1920, 1080);

        Assert.True(FullscreenWindowHeuristics.CoversMonitor(new Rectangle(0, 0, 1920, 1080), monitor));
        Assert.True(FullscreenWindowHeuristics.CoversMonitor(new Rectangle(-10, -10, 1940, 1100), monitor));
    }

    [Fact]
    public void CoversMonitor_AllowsTwoPixelsOfTolerance()
    {
        var monitor = new Rectangle(0, 0, 1920, 1080);

        Assert.True(FullscreenWindowHeuristics.CoversMonitor(new Rectangle(2, 2, 1916, 1076), monitor));
        Assert.False(FullscreenWindowHeuristics.CoversMonitor(new Rectangle(3, 3, 1914, 1074), monitor));
    }

    [Theory]
    [InlineData(3, 0, 1920, 1080)]
    [InlineData(0, 3, 1920, 1080)]
    [InlineData(0, 0, 1917, 1080)]
    [InlineData(0, 0, 1920, 1077)]
    public void CoversMonitor_RejectsGapsOnAnyEdge(int x, int y, int width, int height)
    {
        var monitor = new Rectangle(0, 0, 1920, 1080);

        Assert.False(FullscreenWindowHeuristics.CoversMonitor(new Rectangle(x, y, width, height), monitor));
    }

    [Fact]
    public void CoversMonitor_HandlesMonitorsLeftOfThePrimaryScreen()
    {
        var monitor = new Rectangle(-2560, -200, 2560, 1440);

        Assert.True(FullscreenWindowHeuristics.CoversMonitor(new Rectangle(-2560, -200, 2560, 1440), monitor));
        Assert.False(FullscreenWindowHeuristics.CoversMonitor(new Rectangle(0, 0, 1920, 1080), monitor));
    }

    [Theory]
    [InlineData(0x00C00000u, true)]
    [InlineData(0x00C00000u | 0x00040000u, true)]
    [InlineData(0x00040000u, false)]
    [InlineData(0u, false)]
    public void HasCaption_OnlyLooksAtTheCaptionBit(uint style, bool expected)
    {
        Assert.Equal(expected, FullscreenWindowHeuristics.HasCaption(style));
    }

    [Theory]
    [InlineData(0u, true, true)]
    [InlineData(0x00040000u, true, false)]
    [InlineData(0x00040000u, false, true)]
    [InlineData(0u, false, true)]
    public void IsFullscreenWithoutMaximizeChrome_RejectsBorderedMaximizedWindows(
        uint style,
        bool zoomed,
        bool expected)
    {
        Assert.Equal(expected, FullscreenWindowHeuristics.IsFullscreenWithoutMaximizeChrome(style, zoomed));
    }
}
