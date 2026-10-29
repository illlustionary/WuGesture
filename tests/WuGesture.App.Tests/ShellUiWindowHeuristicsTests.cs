using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ShellUiWindowHeuristicsTests
{
    [Theory]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("SHELLDLL_DefView")]
    [InlineData("Shell_TrayWnd")]
    [InlineData("Shell_SecondaryTrayWnd")]
    [InlineData("MultitaskingViewFrame")]
    [InlineData("TaskListWnd")]
    [InlineData("TaskListThumbnailWnd")]
    [InlineData("TaskSwitcherWnd")]
    [InlineData("XamlExplorerHostIslandWindow")]
    [InlineData("Windows.UI.Core.CoreWindow")]
    public void IsShellUiWindow_DetectsShellWindowClassesRegardlessOfProcess(string className)
    {
        Assert.True(ShellUiWindowHeuristics.IsShellUiWindow(className, "explorer"));
        Assert.True(ShellUiWindowHeuristics.IsShellUiWindow(className, "notepad"));
        Assert.True(ShellUiWindowHeuristics.IsShellWindowClass(className));
    }

    [Theory]
    [InlineData("ShellExperienceHost")]
    [InlineData("StartMenuExperienceHost")]
    [InlineData("SearchUI")]
    [InlineData("SearchHost")]
    [InlineData("TextInputHost")]
    [InlineData("SiHost")]
    [InlineData("ShellHost")]
    [InlineData("LockApp")]
    public void IsShellUiWindow_RejectsEveryWindowOfAShellHostProcess(string processName)
    {
        Assert.True(ShellUiWindowHeuristics.IsShellHostProcess(processName));
        Assert.True(ShellUiWindowHeuristics.IsShellUiWindow("SomeFutureShellWindow", processName));
    }

    [Theory]
    [InlineData("CabinetWClass")]
    [InlineData("ExploreWClass")]
    [InlineData("#32770")]
    public void IsShellUiWindow_AllowsExplorerFolderWindowsAndDialogs(string className)
    {
        Assert.False(ShellUiWindowHeuristics.IsShellUiWindow(className, "explorer"));
        Assert.True(ShellUiWindowHeuristics.IsExplorerApplicationWindowClass(className));
    }

    [Fact]
    public void IsShellUiWindow_TreatsUnknownExplorerWindowsAsShellUi()
    {
        // The whole point of the catch-all: a future shell/XAML window class must still be
        // blocked, because sending it a window control command can crash Explorer.
        Assert.True(ShellUiWindowHeuristics.IsShellUiWindow("SomeFutureShellWindow", "explorer"));
        Assert.True(ShellUiWindowHeuristics.IsShellUiWindow("", "explorer"));
    }

    [Theory]
    [InlineData("Notepad")]
    [InlineData("chrome")]
    [InlineData("ApplicationFrameHost")]
    public void IsShellUiWindow_AllowsRegularApplicationWindows(string processName)
    {
        Assert.False(ShellUiWindowHeuristics.IsShellUiWindow("Chrome_WidgetWin_1", processName));
        Assert.False(ShellUiWindowHeuristics.IsShellUiWindow("ApplicationFrameWindow", processName));
        Assert.False(ShellUiWindowHeuristics.IsShellHostProcess(processName));
    }

    [Fact]
    public void IsShellUiWindow_IgnoresEmptyNames()
    {
        Assert.False(ShellUiWindowHeuristics.IsShellUiWindow("", ""));
        Assert.False(ShellUiWindowHeuristics.IsShellWindowClass(""));
        Assert.False(ShellUiWindowHeuristics.IsShellHostProcess(""));
        Assert.False(ShellUiWindowHeuristics.IsExplorerApplicationWindowClass(""));
    }

    [Fact]
    public void IsShellUiWindow_MatchesNamesCaseInsensitively()
    {
        Assert.True(ShellUiWindowHeuristics.IsShellUiWindow("multitaskingviewframe", ""));
        Assert.True(ShellUiWindowHeuristics.IsShellHostProcess("SHELLEXPERIENCEHOST"));
        Assert.False(ShellUiWindowHeuristics.IsShellUiWindow("cabinetwclass", "Explorer.EXE"));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void IsInteractableWindow_RequiresAVisibleNonCloakedWindow(bool isVisible, bool isCloaked, bool expected)
    {
        Assert.Equal(expected, ShellUiWindowHeuristics.IsInteractableWindow(isVisible, isCloaked));
    }
}
