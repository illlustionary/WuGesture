using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureMatcherTests
{
    private static readonly GestureDirection[] RightPattern = [GestureDirection.Right];
    private static readonly GestureDirection[] UpPattern = [GestureDirection.Up];

    [Fact]
    public void Match_PrefersAppRulesOverCategoryAndGlobalRules()
    {
        var global = CreateRule("global", "Global");
        var category = CreateRule("category:browser", "Browser");
        var app = CreateRule("app:chrome.exe", "Chrome");
        var matcher = new GestureMatcher([global, category, app]);

        var result = matcher.Match(RightPattern, new GestureScopeContext("Chrome.exe", ["browser"]));

        Assert.Same(app, result);
    }

    [Fact]
    public void Match_PrefersTheFirstAssociatedCategory()
    {
        var firstCategory = CreateRule("category:work", "Work");
        var laterCategory = CreateRule("category:browser", "Browser");
        var matcher = new GestureMatcher([firstCategory, laterCategory]);

        var result = matcher.Match(RightPattern, new GestureScopeContext("chrome.exe", ["work", "browser"]));

        Assert.Same(firstCategory, result);
    }

    [Fact]
    public void Match_RequiresTheSameMouseButtonAndWholePattern()
    {
        var middleButtonRule = CreateRule("global", "Middle", GestureMouseButton.Middle);
        var matcher = new GestureMatcher([middleButtonRule]);

        Assert.Null(matcher.Match(RightPattern));
        Assert.Null(matcher.Match([GestureDirection.Right, GestureDirection.Down], button: GestureMouseButton.Middle));
        Assert.Same(middleButtonRule, matcher.Match(RightPattern, button: GestureMouseButton.Middle));
    }

    [Fact]
    public void Match_IgnoresRulesThatRequireAWheelModifier()
    {
        var plain = CreateRule("global", "Plain");
        var wheel = CreateWheelRule("global", "Wheel", UpPattern, GestureWheelDirection.Up);
        var matcher = new GestureMatcher([wheel, plain]);

        Assert.Same(plain, matcher.Match(RightPattern));
        Assert.Null(matcher.Match(UpPattern));
        Assert.Null(new GestureMatcher([wheel]).Match(UpPattern));
    }

    [Fact]
    public void MatchWheel_RequiresTheSamePatternDirectionButtonAndScopePriority()
    {
        var global = CreateWheelRule("global", "Global", UpPattern, GestureWheelDirection.Up);
        var category = CreateWheelRule("category:work", "Work", UpPattern, GestureWheelDirection.Up);
        var down = CreateWheelRule("global", "Down", UpPattern, GestureWheelDirection.Down);
        var otherPattern = CreateWheelRule("global", "Other pattern", RightPattern, GestureWheelDirection.Up);
        var matcher = new GestureMatcher([global, category, down, otherPattern]);

        Assert.Same(
            category,
            matcher.MatchWheel(GestureWheelDirection.Up, UpPattern, new GestureScopeContext("chrome.exe", ["work"])));
        Assert.Same(
            global,
            matcher.MatchWheel(GestureWheelDirection.Up, UpPattern, new GestureScopeContext("chrome.exe", ["other"])));
        Assert.Same(down, matcher.MatchWheel(GestureWheelDirection.Down, UpPattern));
        Assert.Null(matcher.MatchWheel(GestureWheelDirection.Up, RightPattern, button: GestureMouseButton.Middle));
        Assert.Null(matcher.MatchWheel(GestureWheelDirection.Up, [GestureDirection.Down]));
    }

    [Fact]
    public void MatchWheel_SupportsHoldAndScrollRulesWithAnEmptyPattern()
    {
        var holdAndScroll = CreateWheelRule("global", "Wheel", [], GestureWheelDirection.Up);
        var matcher = new GestureMatcher([holdAndScroll]);

        Assert.Same(holdAndScroll, matcher.MatchWheel(GestureWheelDirection.Up, []));
        Assert.Null(matcher.MatchWheel(GestureWheelDirection.Down, []));
        Assert.Null(matcher.MatchWheel(GestureWheelDirection.Up, UpPattern));
        Assert.Null(matcher.Match([]));
    }

    [Fact]
    public void HasWheelRules_TracksWheelModifiersPerMouseButton()
    {
        var directionRule = CreateRule("global", "Direction");
        var rightWheel = CreateWheelRule("global", "Right wheel", UpPattern, GestureWheelDirection.Up);
        var matcher = new GestureMatcher([directionRule, rightWheel]);

        Assert.True(matcher.HasWheelRules(GestureMouseButton.Right));
        Assert.False(matcher.HasWheelRules(GestureMouseButton.Middle));
        Assert.False(new GestureMatcher([directionRule]).HasWheelRules(GestureMouseButton.Right));
    }

    private static GestureRule CreateWheelRule(
        string scope,
        string actionName,
        IReadOnlyList<GestureDirection> pattern,
        GestureWheelDirection modifier,
        GestureMouseButton mouseButton = GestureMouseButton.Right)
    {
        return new GestureRule(
            pattern,
            scope,
            actionName,
            new WindowControlAction(WindowControlOperation.Minimize),
            mouseButton)
        {
            WheelModifier = modifier,
            ExecuteOnWheel = true
        };
    }

    private static GestureRule CreateRule(
        string scope,
        string actionName,
        GestureMouseButton mouseButton = GestureMouseButton.Right)
    {
        return new GestureRule(
            RightPattern,
            scope,
            actionName,
            new WindowControlAction(WindowControlOperation.Minimize),
            mouseButton);
    }
}
