using System.Windows.Forms;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureConfigMapperTests
{
    [Fact]
    public void ToRules_DropsRulesWithoutPatternOrWithInvalidValues()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig { Pattern = [] },
                new GestureRuleConfig { Pattern = ["Down", "Sideways"] },
                new GestureRuleConfig
                {
                    Pattern = ["Down"],
                    Action = new GestureActionConfig { Type = GestureConfigContract.ActionTypes.Window, Operation = "explode" }
                },
                new GestureRuleConfig
                {
                    Pattern = ["Down"],
                    Action = new GestureActionConfig
                    {
                        Type = GestureConfigContract.ActionTypes.Hotkey,
                        Keys = ["Control", "W"]
                    }
                }
            ]
        };

        var rules = GestureConfigMapper.ToRules(config);

        var rule = Assert.Single(rules);
        var direction = Assert.Single(rule.Pattern);
        Assert.Equal(GestureDirection.Down, direction);
        Assert.IsType<HotkeyAction>(rule.Action);
    }

    [Fact]
    public void ToRules_UsesTheKeyListAsTheDefaultActionName()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Pattern = ["Left"],
                    Action = new GestureActionConfig
                    {
                        Type = GestureConfigContract.ActionTypes.Hotkey,
                        Keys = ["Alt", "Left"]
                    }
                }
            ]
        };

        var rule = Assert.Single(GestureConfigMapper.ToRules(config));

        Assert.Equal("Alt + Left", rule.ActionName);
        Assert.Equal(GestureConfigContract.Scopes.Global, rule.Scope);
        Assert.Equal(GestureMouseButton.Right, rule.MouseButton);
    }

    [Fact]
    public void ToRules_TrimsProgramPathAndArguments()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Pattern = ["Up"],
                    MouseButton = GestureConfigContract.MouseButtons.Middle,
                    Action = new GestureActionConfig
                    {
                        Type = GestureConfigContract.ActionTypes.Program,
                        Path = @" C:\Tools\a.exe ",
                        Arguments = [" --x ", "", "   "]
                    }
                }
            ]
        };

        var rule = Assert.Single(GestureConfigMapper.ToRules(config));
        var action = Assert.IsType<ProgramAction>(rule.Action);

        Assert.Equal(@"C:\Tools\a.exe", action.Path);
        var argument = Assert.Single(action.Arguments);
        Assert.Equal("--x", argument);
        Assert.Equal(GestureMouseButton.Middle, rule.MouseButton);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-3, 5)]
    [InlineData(30, 30)]
    [InlineData(150, 100)]
    public void ToAction_ClampsTheVolumeAmount(int amount, int expected)
    {
        var config = new GestureActionConfig
        {
            Type = GestureConfigContract.ActionTypes.Volume,
            Operation = GestureConfigContract.Operations.Increase,
            Amount = amount
        };

        var action = Assert.IsType<VolumeControlAction>(GestureConfigMapper.ToAction(config));

        Assert.Equal(VolumeControlOperation.Increase, action.Operation);
        Assert.Equal(expected, action.Amount);
    }

    [Fact]
    public void ToAction_RejectsIncompleteActions()
    {
        Assert.Null(GestureConfigMapper.ToAction(new GestureActionConfig
        {
            Type = GestureConfigContract.ActionTypes.Hotkey,
            Keys = []
        }));
        Assert.Null(GestureConfigMapper.ToAction(new GestureActionConfig
        {
            Type = GestureConfigContract.ActionTypes.Program,
            Path = "  "
        }));
        Assert.Null(GestureConfigMapper.ToAction(new GestureActionConfig
        {
            Type = GestureConfigContract.ActionTypes.Window,
            Operation = "explode"
        }));
        Assert.Null(GestureConfigMapper.ToAction(new GestureActionConfig
        {
            Type = "nonsense"
        }));
    }

    [Fact]
    public void FromRules_PreservesScopeButtonAndHotkey()
    {
        var rule = new GestureRule(
            [GestureDirection.Down, GestureDirection.Right],
            "category:工具",
            "关闭窗口",
            new HotkeyAction([Keys.Alt, Keys.F4]),
            GestureMouseButton.Middle);

        var config = GestureConfigMapper.FromRules([rule]);

        Assert.Equal(GestureConfigContract.Schema.CurrentVersion, config.SchemaVersion);
        var mapped = Assert.Single(config.Rules);
        Assert.Equal("category:工具", mapped.Scope);
        Assert.Equal(GestureConfigContract.MouseButtons.Middle, mapped.MouseButton);
        Assert.Equal(GestureConfigContract.ActionTypes.Hotkey, mapped.Action.Type);
        Assert.Equal(2, mapped.Action.Keys.Count);
        Assert.Equal("Alt", mapped.Action.Keys[0]);
        Assert.Equal("F4", mapped.Action.Keys[1]);
    }

    [Fact]
    public void FromRules_NormalizesVolumeAmountAndOperationNames()
    {
        var rule = new GestureRule(
            [GestureDirection.Up],
            GestureConfigContract.Scopes.Global,
            "音量",
            new VolumeControlAction(VolumeControlOperation.Mute, 0));

        var config = GestureConfigMapper.FromRules([rule]);

        var action = Assert.Single(config.Rules).Action;
        Assert.Equal(GestureConfigContract.ActionTypes.Volume, action.Type);
        Assert.Equal(GestureConfigContract.Operations.Mute, action.Operation);
        Assert.Equal(5, action.Amount);
    }
}
