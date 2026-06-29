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
}
