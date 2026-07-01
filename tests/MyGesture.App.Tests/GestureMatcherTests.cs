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

    [Fact]
    public void Match_FallsBackToGlobalWhenAppAndCategoryDoNotMatch()
    {
        var matcher = new GestureMatcher(
        [
            new([GestureDirection.Down], "global", "Global Close", new HotkeyAction([])),
            new([GestureDirection.Down], "category:Browser", "Browser Close", new HotkeyAction([])),
            new([GestureDirection.Down], "app:msedge", "Edge Close", new HotkeyAction([]))
        ]);

        var rule = matcher.Match([GestureDirection.Down], new GestureScopeContext("notepad", "Edit"));

        Assert.NotNull(rule);
        Assert.Equal("Global Close", rule!.ActionName);
    }

    [Fact]
    public void Match_SupportsCaseInsensitivePrefixedScopes()
    {
        var matcher = new GestureMatcher(
        [
            new([GestureDirection.Up], " CATEGORY: browser ", "Browser Up", new HotkeyAction([])),
            new([GestureDirection.Up], " APP: MSEDGE ", "Edge Up", new HotkeyAction([]))
        ]);

        var rule = matcher.Match([GestureDirection.Up], new GestureScopeContext("msedge", "Browser"));

        Assert.NotNull(rule);
        Assert.Equal("Edge Up", rule!.ActionName);
    }

    [Fact]
    public void Match_SupportsLegacyUnprefixedAppAndCategoryScopes()
    {
        var matcher = new GestureMatcher(
        [
            new([GestureDirection.UpLeft], "Browser", "Browser Diagonal", new HotkeyAction([])),
            new([GestureDirection.UpLeft], "msedge", "Edge Diagonal", new HotkeyAction([]))
        ]);

        var rule = matcher.Match([GestureDirection.UpLeft], new GestureScopeContext("msedge", "Browser"));

        Assert.NotNull(rule);
        Assert.Equal("Edge Diagonal", rule!.ActionName);
    }

    [Fact]
    public void Match_FiltersByMouseButton()
    {
        var matcher = new GestureMatcher(
        [
            new([GestureDirection.Left], "global", "Right Back", new HotkeyAction([]), GestureMouseButton.Right),
            new([GestureDirection.Left], "global", "Middle Back", new HotkeyAction([]), GestureMouseButton.Middle)
        ]);

        var rightRule = matcher.Match([GestureDirection.Left], GestureScopeContext.Empty, GestureMouseButton.Right);
        var middleRule = matcher.Match([GestureDirection.Left], GestureScopeContext.Empty, GestureMouseButton.Middle);

        Assert.NotNull(rightRule);
        Assert.NotNull(middleRule);
        Assert.Equal("Right Back", rightRule!.ActionName);
        Assert.Equal("Middle Back", middleRule!.ActionName);
    }

    [Fact]
    public void Match_DoesNotUseRightButtonRuleForMiddleButton()
    {
        var matcher = new GestureMatcher(
        [
            new([GestureDirection.Right], "global", "Right Forward", new HotkeyAction([]), GestureMouseButton.Right)
        ]);

        var rule = matcher.Match([GestureDirection.Right], GestureScopeContext.Empty, GestureMouseButton.Middle);

        Assert.Null(rule);
    }
}
