using MyGesture.App.GestureEngine;
using System.Windows.Forms;

namespace MyGesture.App.Tests;

public sealed class GestureConfigMapperTests
{
    [Fact]
    public void ToRules_MapsDefaultConfig()
    {
        var config = GestureConfigMapper.FromRules(DefaultGestureRules.Create());

        var rules = GestureConfigMapper.ToRules(config);

        Assert.Equal(3, rules.Count);
        Assert.Equal([GestureDirection.Left], rules[0].Pattern);
        Assert.Equal([Keys.Menu, Keys.Left], rules[0].Action.Keys);
    }

    [Fact]
    public void ToRules_SupportsKeyAliases()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Scope = "global",
                    Pattern = ["Down", "Right"],
                    ActionName = "Close Tab",
                    Action = new GestureActionConfig
                    {
                        Type = "hotkey",
                        Keys = ["Ctrl", "W"]
                    }
                }
            ]
        };

        var rules = GestureConfigMapper.ToRules(config);

        Assert.Single(rules);
        Assert.Equal([GestureDirection.Down, GestureDirection.Right], rules[0].Pattern);
        Assert.Equal([Keys.ControlKey, Keys.W], rules[0].Action.Keys);
    }

    [Fact]
    public void FromRules_KeepsScopeValues()
    {
        var config = GestureConfigMapper.FromRules(
        [
            new([GestureDirection.Left], "app:msedge", "Back", new HotkeyAction([Keys.Menu, Keys.Left])),
            new([GestureDirection.Right], "category:Browser", "Forward", new HotkeyAction([Keys.Menu, Keys.Right]))
        ]);

        Assert.Equal("app:msedge", config.Rules[0].Scope);
        Assert.Equal("category:Browser", config.Rules[1].Scope);
    }

    [Fact]
    public void ToRules_KeepsScopeValues()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Scope = " app:msedge ",
                    Pattern = ["Left"],
                    ActionName = "Edge Back",
                    Action = new GestureActionConfig
                    {
                        Type = "hotkey",
                        Keys = ["Alt", "Left"]
                    }
                },
                new GestureRuleConfig
                {
                    Scope = "category:Browser",
                    Pattern = ["Right"],
                    ActionName = "Browser Forward",
                    Action = new GestureActionConfig
                    {
                        Type = "hotkey",
                        Keys = ["Alt", "Right"]
                    }
                }
            ]
        };

        var rules = GestureConfigMapper.ToRules(config);

        Assert.Equal(2, rules.Count);
        Assert.Equal("app:msedge", rules[0].Scope);
        Assert.Equal("category:Browser", rules[1].Scope);
    }

    [Fact]
    public void ToRules_IgnoresRulesWithoutHotkeyKeys()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Scope = "global",
                    Pattern = ["Left"],
                    ActionName = "◑←",
                    Action = new GestureActionConfig
                    {
                        Type = "hotkey",
                        Keys = []
                    }
                }
            ]
        };

        var rules = GestureConfigMapper.ToRules(config);

        Assert.Empty(rules);
    }

    [Fact]
    public void ToRules_PreservesMiddleButtonRulesForRuntimeExecution()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Scope = "global",
                    MouseButton = "middle",
                    Pattern = ["DownLeft"],
                    ActionName = "●↙",
                    Action = new GestureActionConfig
                    {
                        Type = "hotkey",
                        Keys = ["Win", "D"]
                    }
                }
            ]
        };

        var rules = GestureConfigMapper.ToRules(config);

        Assert.Single(rules);
        Assert.Equal(GestureMouseButton.Middle, rules[0].MouseButton);
        Assert.Equal([GestureDirection.DownLeft], rules[0].Pattern);
    }
}
