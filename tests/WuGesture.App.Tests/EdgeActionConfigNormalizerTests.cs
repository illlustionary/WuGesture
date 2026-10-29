using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class EdgeActionConfigNormalizerTests
{
    private static EdgeActionConfig Action(
        string triggerType,
        string location,
        GestureActionConfig? action = null,
        string wheelDirection = "",
        int frictionCount = 4)
    {
        return new EdgeActionConfig
        {
            Enabled = true,
            TriggerType = triggerType,
            Location = location,
            WheelDirection = wheelDirection,
            FrictionCount = frictionCount,
            Action = action ?? new GestureActionConfig()
        };
    }

    [Fact]
    public void Normalize_DropsEntriesWithoutTriggerTypeOrLocation()
    {
        var result = EdgeActionConfigNormalizer.Normalize(
        [
            Action("", "left"),
            Action("friction", " "),
            Action(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Left)
        ]);

        Assert.Single(result);
    }

    [Fact]
    public void Normalize_DowngradesProgramActionsToAnEmptyHotkey()
    {
        var action = Action(
            GestureConfigContract.EdgeTriggerTypes.Corner,
            GestureConfigContract.EdgeLocations.TopLeft,
            new GestureActionConfig
            {
                Type = "PROGRAM",
                Path = @"C:\Tools\a.exe",
                Arguments = ["--flag"],
                Keys = ["Alt", "Tab"]
            });

        var result = Assert.Single(EdgeActionConfigNormalizer.Normalize([action]));

        Assert.Equal(GestureConfigContract.ActionTypes.Hotkey, result.Action.Type);
        Assert.Empty(result.Action.Keys);
        Assert.Equal("", result.Action.Path);
        Assert.Empty(result.Action.Arguments);
    }

    [Fact]
    public void Normalize_ResetsInvalidFrictionAndWheelLocationsToTheLeftEdge()
    {
        var result = EdgeActionConfigNormalizer.Normalize(
        [
            Action(GestureConfigContract.EdgeTriggerTypes.Friction, "top-left"),
            Action(GestureConfigContract.EdgeTriggerTypes.Wheel, "center")
        ]);

        Assert.Equal(GestureConfigContract.EdgeLocations.Left, result[0].Location);
        Assert.Equal(GestureConfigContract.EdgeLocations.Left, result[1].Location);
    }

    [Theory]
    [InlineData("LEFT")]
    [InlineData(" top ")]
    [InlineData("right")]
    public void Normalize_KeepsValidEdgeLocations(string location)
    {
        var result = Assert.Single(EdgeActionConfigNormalizer.Normalize(
            [Action(GestureConfigContract.EdgeTriggerTypes.Friction, location)]));

        Assert.Equal(location, result.Location);
    }

    [Fact]
    public void Normalize_LeavesCornerActionsAndOtherFieldsUntouched()
    {
        var action = Action(
            GestureConfigContract.EdgeTriggerTypes.Corner,
            GestureConfigContract.EdgeLocations.BottomRight,
            new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win", "D"]
            });

        var result = Assert.Single(EdgeActionConfigNormalizer.Normalize([action]));

        Assert.Equal(GestureConfigContract.EdgeLocations.BottomRight, result.Location);
        Assert.Equal(["Win", "D"], result.Action.Keys);
        Assert.True(result.Enabled);
    }

    [Fact]
    public void Normalize_HandlesMissingSources()
    {
        Assert.Empty(EdgeActionConfigNormalizer.Normalize(null));
        Assert.Empty(EdgeActionConfigNormalizer.Normalize([]));
    }
}
