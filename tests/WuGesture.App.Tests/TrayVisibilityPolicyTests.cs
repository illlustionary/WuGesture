namespace WuGesture.App.Tests;

public sealed class TrayVisibilityPolicyTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void ShouldHideToTray_HidesOnlyOnceAndNeverWhileClosing(
        bool alreadyHidden,
        bool isClosing,
        bool expected)
    {
        Assert.Equal(expected, TrayVisibilityPolicy.ShouldHideToTray(alreadyHidden, isClosing));
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    public void ShouldKeepHidden_RefusesExternalShowsOnlyWhileHiddenToTray(
        bool isHiddenToTray,
        bool isRestoring,
        bool isClosing,
        bool expected)
    {
        Assert.Equal(expected, TrayVisibilityPolicy.ShouldKeepHidden(isHiddenToTray, isRestoring, isClosing));
    }

    [Fact]
    public void ShouldKeepHidden_AllowsRestoreStartedFromTray()
    {
        // RestoreFromTray clears the hidden flag before rebuilding the WebView2 host, so the guard must let
        // the intentional show through instead of hiding the window again.
        Assert.False(TrayVisibilityPolicy.ShouldKeepHidden(
            isHiddenToTray: false,
            isRestoring: true,
            isClosing: false));
    }
}
