namespace WuGesture.App.Tests;

public sealed class TitleBarLayoutTests
{
    [Fact]
    public void Scale_RoundsLogicalPixelsForTheCurrentDpi()
    {
        Assert.Equal(40, TitleBarLayout.Scale(TitleBarLayout.LogicalHeight, 96));
        Assert.Equal(50, TitleBarLayout.Scale(TitleBarLayout.LogicalHeight, 120));
        Assert.Equal(58, TitleBarLayout.Scale(TitleBarLayout.LogicalButtonWidth, 120));
        Assert.Equal(TitleBarLayout.LogicalHeight, TitleBarLayout.Scale(TitleBarLayout.LogicalHeight, 0));
    }

    [Fact]
    public void GetButtonBounds_StacksCloseMaximizeMinimizeFromTheRightEdge()
    {
        const int width = 1000;
        const int height = 40;
        const int dpi = 96;

        var close = TitleBarLayout.GetButtonBounds(width, height, dpi, TitleBarButton.Close);
        var maximize = TitleBarLayout.GetButtonBounds(width, height, dpi, TitleBarButton.Maximize);
        var minimize = TitleBarLayout.GetButtonBounds(width, height, dpi, TitleBarButton.Minimize);

        Assert.Equal(new Rectangle(width - 46, 0, 46, height), close);
        Assert.Equal(close.Left - 46, maximize.Left);
        Assert.Equal(maximize.Left - 46, minimize.Left);
        Assert.Equal(Rectangle.Empty, TitleBarLayout.GetButtonBounds(width, height, dpi, TitleBarButton.None));
    }

    [Fact]
    public void HitTestButton_ReportsTheButtonUnderThePoint()
    {
        const int width = 1000;
        const int height = 40;
        const int dpi = 120;
        var buttonWidth = TitleBarLayout.Scale(TitleBarLayout.LogicalButtonWidth, dpi);

        Assert.Equal(TitleBarButton.Close, TitleBarLayout.HitTestButton(width, height, dpi, new Point(width - 1, height / 2)));
        Assert.Equal(
            TitleBarButton.Maximize,
            TitleBarLayout.HitTestButton(width, height, dpi, new Point(width - buttonWidth - 1, height / 2)));
        Assert.Equal(
            TitleBarButton.Minimize,
            TitleBarLayout.HitTestButton(width, height, dpi, new Point(width - buttonWidth * 2 - 1, height / 2)));
        Assert.Equal(TitleBarButton.None, TitleBarLayout.HitTestButton(width, height, dpi, new Point(10, height / 2)));
        Assert.Equal(TitleBarButton.None, TitleBarLayout.HitTestButton(width, height, dpi, new Point(width - 1, height)));
        Assert.Equal(TitleBarButton.None, TitleBarLayout.HitTestButton(width, height, dpi, new Point(width, 1)));
    }

    [Fact]
    public void GetTextBounds_StaysBetweenTheIconAndTheWindowButtons()
    {
        const int width = 800;
        const int height = 40;
        const int dpi = 96;
        var icon = TitleBarLayout.GetIconBounds(height, dpi);
        var text = TitleBarLayout.GetTextBounds(width, height, dpi);
        var minimize = TitleBarLayout.GetButtonBounds(width, height, dpi, TitleBarButton.Minimize);

        Assert.True(text.Left > icon.Right);
        Assert.True(text.Right <= minimize.Left);
        Assert.Equal(height, text.Height);
        Assert.Equal(0, TitleBarLayout.GetIconBounds(height, dpi).Top + icon.Height / 2 - height / 2);
    }

    [Fact]
    public void GetTextBounds_CollapsesOnWindowsNarrowerThanTheChrome()
    {
        var text = TitleBarLayout.GetTextBounds(60, 40, 96);

        Assert.Equal(0, text.Width);
    }
}
