using MyGesture.App.GestureEngine;

namespace MyGesture.App.Tests;

public sealed class GestureMatcherTests
{
    [Fact]
    public void Match_PrefersAppScopeOverCategoryAndGlobal()
    {
        var matcher = new GestureMatcher(
        [
            new([GestureDirection.Left], "global", "Global Back", new HotkeyAction([])),
            new([GestureDirection.Left], "category:Browser", "Browser Back", new HotkeyAction([])),
            new([GestureDirection.Left], "app:msedge", "Edge Back", new HotkeyAction([]))
        ]);

        var rule = matcher.Match([GestureDirection.Left], new GestureScopeContext("msedge", "Browser"));

        Assert.NotNull(rule);
        Assert.Equal("Edge Back", rule!.ActionName);
    }

    [Fact]
    public void Match_FallsBackToCategoryThenGlobal()
    {
        var matcher = new GestureMatcher(
        [
            new([GestureDirection.Right], "global", "Global Forward", new HotkeyAction([])),
            new([GestureDirection.Right], "category:Browser", "Browser Forward", new HotkeyAction([]))
        ]);

        var rule = matcher.Match([GestureDirection.Right], new GestureScopeContext("notepad", "Browser"));

        Assert.NotNull(rule);
        Assert.Equal("Browser Forward", rule!.ActionName);
    }
}
