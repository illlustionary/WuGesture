namespace WuGesture.App.Tests;

public sealed class WindowStateStoreTests
{
    private static readonly Rectangle FullHdWorkArea = new(0, 0, 1920, 1040);

    [Theory]
    [InlineData(1280, 96, 1280)]
    [InlineData(1280, 120, 1600)]
    [InlineData(1280, 144, 1920)]
    [InlineData(720, 120, 900)]
    [InlineData(720, 0, 720)]
    public void Scale_UsesTheDpiRatio(int logicalPixels, int dpi, int expected)
    {
        Assert.Equal(expected, WindowStateStore.Scale(logicalPixels, dpi));
    }

    [Theory]
    [InlineData(96, 1280, 720)]
    [InlineData(120, 1600, 900)]
    public void GetDefaultBounds_ScalesTheLogicalSizeByDpi(int dpi, int expectedWidth, int expectedHeight)
    {
        var bounds = WindowStateStore.GetDefaultBounds(dpi, FullHdWorkArea);

        Assert.Equal(new Size(expectedWidth, expectedHeight), bounds.Size);
        Assert.Equal((FullHdWorkArea.Width - expectedWidth) / 2, bounds.Left);
        Assert.Equal((FullHdWorkArea.Height - expectedHeight) / 2, bounds.Top);
    }

    [Fact]
    public void GetDefaultBounds_ClampsToTheWorkingAreaAt150Percent()
    {
        var bounds = WindowStateStore.GetDefaultBounds(144, FullHdWorkArea);

        Assert.Equal(FullHdWorkArea, bounds);
    }

    [Fact]
    public void GetDefaultBounds_ClampsToSmallScreens()
    {
        var smallWorkArea = new Rectangle(0, 0, 1366, 728);

        var bounds = WindowStateStore.GetDefaultBounds(120, smallWorkArea);

        Assert.Equal(smallWorkArea, bounds);
    }

    [Fact]
    public void GetMinimumSize_ScalesAndClampsToTheWorkingArea()
    {
        Assert.Equal(new Size(1280, 720), WindowStateStore.GetMinimumSize(96, FullHdWorkArea));
        Assert.Equal(new Size(1920, 1040), WindowStateStore.GetMinimumSize(144, FullHdWorkArea));
        Assert.Equal(
            new Size(1366, 728),
            WindowStateStore.GetMinimumSize(120, new Rectangle(0, 0, 1366, 728)));
    }

    [Fact]
    public void NormalizeBounds_RaisesLegacyPhysicalSizesToTheScaledMinimum()
    {
        var normalized = WindowStateStore.NormalizeBounds(new Rectangle(100, 100, 1000, 600), 144, FullHdWorkArea);

        Assert.Equal(new Rectangle(0, 0, 1920, 1040), normalized);
    }

    [Fact]
    public void NormalizeBounds_CentersWindowsThatNoLongerTouchTheWorkingArea()
    {
        var normalized = WindowStateStore.NormalizeBounds(new Rectangle(5000, 5000, 1280, 720), 96, FullHdWorkArea);

        Assert.Equal(new Rectangle(320, 160, 1280, 720), normalized);
    }

    [Fact]
    public void NormalizeBounds_KeepsWindowsOnASecondaryMonitor()
    {
        var secondaryWorkArea = new Rectangle(-1920, 0, 1920, 1040);

        var normalized = WindowStateStore.NormalizeBounds(
            new Rectangle(-1800, 100, 1280, 720),
            96,
            secondaryWorkArea);

        Assert.Equal(new Rectangle(-1800, 100, 1280, 720), normalized);
    }

    [Fact]
    public void NormalizeBounds_ShrinksWindowsLargerThanTheWorkingArea()
    {
        var normalized = WindowStateStore.NormalizeBounds(new Rectangle(0, 0, 3000, 2000), 96, FullHdWorkArea);

        Assert.Equal(FullHdWorkArea, normalized);
    }
}
