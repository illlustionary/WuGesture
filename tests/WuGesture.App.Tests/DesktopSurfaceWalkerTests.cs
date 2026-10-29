using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class DesktopSurfaceWalkerTests
{
    private static bool IsDesktopSurface(
        IntPtr window,
        Func<IntPtr, string> windowClassName,
        Func<IntPtr, IntPtr>? parent = null,
        Func<IntPtr, IntPtr>? owner = null,
        Func<IntPtr, IntPtr>? root = null,
        Func<IntPtr, IntPtr>? rootOwner = null)
    {
        return DesktopSurfaceWalker.IsDesktopSurface(
            window,
            windowClassName,
            parent ?? (_ => IntPtr.Zero),
            owner ?? (_ => IntPtr.Zero),
            root ?? (_ => IntPtr.Zero),
            rootOwner ?? (_ => IntPtr.Zero));
    }

    [Fact]
    public void IsDesktopSurface_ReturnsFalseForTheNullWindow()
    {
        Assert.False(IsDesktopSurface(IntPtr.Zero, _ => "WorkerW"));
    }

    [Theory]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("SHELLDLL_DefView")]
    public void IsDesktopSurface_DetectsShellClassesOnTheWindowItself(string className)
    {
        Assert.True(IsDesktopSurface((IntPtr)1, _ => className));
    }

    [Fact]
    public void IsDesktopSurface_WalksUpTheParentChain()
    {
        Assert.True(IsDesktopSurface(
            (IntPtr)1,
            window => window == (IntPtr)2 ? "WorkerW" : "Chrome_WidgetWin_1",
            parent: window => window == (IntPtr)1 ? (IntPtr)2 : IntPtr.Zero));
    }

    [Fact]
    public void IsDesktopSurface_FollowsOwnedWindows()
    {
        Assert.True(IsDesktopSurface(
            (IntPtr)1,
            window => window == (IntPtr)2 ? "Progman" : "NotDesktop",
            owner: window => window == (IntPtr)1 ? (IntPtr)2 : IntPtr.Zero));
    }

    [Fact]
    public void IsDesktopSurface_DetectsShellClassesAmongRootAncestors()
    {
        Assert.True(IsDesktopSurface(
            (IntPtr)1,
            window => window == (IntPtr)2 ? "SHELLDLL_DefView" : "NotDesktop",
            root: window => window == (IntPtr)1 ? (IntPtr)2 : IntPtr.Zero));
    }

    [Fact]
    public void IsDesktopSurface_ReturnsFalseWhenNothingMatches()
    {
        Assert.False(IsDesktopSurface(
            (IntPtr)1,
            _ => "Notepad",
            parent: window => window == (IntPtr)1 ? (IntPtr)2 : IntPtr.Zero));
    }

    [Fact]
    public void IsDesktopSurface_StopsOnCyclicParentChains()
    {
        var visits = 0;

        var result = IsDesktopSurface(
            (IntPtr)1,
            _ =>
            {
                visits++;
                return "Notepad";
            },
            parent: window => window == (IntPtr)1 ? (IntPtr)2 : (IntPtr)1);

        Assert.False(result);
        Assert.Equal(2, visits);
    }

    [Fact]
    public void IsDesktopSurface_VisitsAtMostTheRelatedWindowLimit()
    {
        Func<IntPtr, IntPtr> parent = window => window < (IntPtr)40 ? window + 1 : IntPtr.Zero;

        Assert.True(IsDesktopSurface(
            (IntPtr)1,
            window => window == (IntPtr)32 ? "WorkerW" : "Notepad",
            parent: parent));

        Assert.False(IsDesktopSurface(
            (IntPtr)1,
            window => window == (IntPtr)33 ? "WorkerW" : "Notepad",
            parent: parent));
    }

    [Theory]
    [InlineData("Progman", true)]
    [InlineData("WorkerW", true)]
    [InlineData("SHELLDLL_DefView", true)]
    [InlineData("progman", false)]
    [InlineData("Notepad", false)]
    [InlineData("", false)]
    public void IsDesktopShellClass_OnlyMatchesKnownShellClasses(string className, bool expected)
    {
        Assert.Equal(expected, DesktopSurfaceWalker.IsDesktopShellClass(className));
    }
}
