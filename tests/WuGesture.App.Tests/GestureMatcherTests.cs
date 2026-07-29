using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureMatcherTests
{
    private static readonly GestureDirection[] RightPattern = [GestureDirection.Right];

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
