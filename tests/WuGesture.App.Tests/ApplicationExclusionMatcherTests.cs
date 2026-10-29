using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ApplicationExclusionMatcherTests
{
    private static ExcludedApplicationConfig Rule(
        string name = "",
        string path = "",
        string displayName = "",
        bool disableEdgeActions = false)
    {
        return new ExcludedApplicationConfig
        {
            Name = name,
            Path = path,
            DisplayName = displayName,
            DisableEdgeActions = disableEdgeActions
        };
    }

    [Fact]
    public void IsMatch_MatchesOnTheSameExecutablePath()
    {
        var rule = Rule(name: "notepad", path: @"C:\Tools\notepad.exe");

        Assert.True(ApplicationExclusionMatcher.IsMatch(rule, "other", @"c:\tools\NOTEPAD.exe"));
    }

    [Fact]
    public void IsMatch_FallsBackToTheProcessName()
    {
        var rule = Rule(name: "notepad", path: @"C:\Tools\notepad.exe");

        Assert.True(ApplicationExclusionMatcher.IsMatch(rule, "Notepad", @"D:\Portable\notepad.exe"));
        Assert.True(ApplicationExclusionMatcher.IsMatch(rule, "notepad", ""));
    }

    [Fact]
    public void IsMatch_RejectsDifferentApplications()
    {
        var rule = Rule(name: "notepad", path: @"C:\Tools\notepad.exe");

        Assert.False(ApplicationExclusionMatcher.IsMatch(rule, "code", @"C:\Tools\code.exe"));
    }

    [Fact]
    public void IsMatch_ComparesNamesWithoutDirectoryOrExtension()
    {
        var rule = Rule(name: @"C:\Windows\notepad.exe");

        Assert.True(ApplicationExclusionMatcher.IsMatch(rule, "notepad", ""));
        Assert.False(ApplicationExclusionMatcher.IsMatch(rule, "notepad-plus-plus", ""));
    }

    [Fact]
    public void IsMatch_IgnoresEmptyRules()
    {
        Assert.False(ApplicationExclusionMatcher.IsMatch(Rule(), "", ""));
        Assert.False(ApplicationExclusionMatcher.IsMatch(Rule(path: @"C:\Tools\notepad.exe"), "", ""));
    }

    [Fact]
    public void NormalizeApplications_DropsEmptyEntriesAndTrimsTheRest()
    {
        var rules = ApplicationExclusionMatcher.NormalizeApplications(
        [
            Rule(name: "  ", path: "  "),
            Rule(name: @" C:\Tools\notepad.exe ", path: " C:\\Tools\\notepad.exe ", displayName: " 记事本 ", disableEdgeActions: true),
            Rule(path: @"D:\Portable\code.exe")
        ]);

        Assert.Equal(2, rules.Count);
        Assert.Equal("notepad", rules[0].Name);
        Assert.Equal(@"C:\Tools\notepad.exe", rules[0].Path);
        Assert.Equal("记事本", rules[0].DisplayName);
        Assert.True(rules[0].DisableEdgeActions);
        // 只有路径的排除项保留空名称，运行时按路径匹配。
        Assert.Equal("", rules[1].Name);
        Assert.Equal(@"D:\Portable\code.exe", rules[1].Path);
    }

    [Fact]
    public void NormalizeApplications_ReturnsAnEmptyListForMissingSource()
    {
        Assert.Empty(ApplicationExclusionMatcher.NormalizeApplications(null));
    }
}
