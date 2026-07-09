namespace WuGesture.App.GestureEngine;

public static class DefaultGestureConfig
{
    public static GestureConfig Create()
    {
        var config = GestureConfigMapper.FromRules(DefaultGestureRules.Create());
        config.EdgeActions =
        [
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.TopLeft, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.TopRight, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.BottomLeft, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.BottomRight, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win", "D"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Left, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Control", "Shift", "Escape"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Right, "", new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Top, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["M", "I", "K", "U"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Bottom, "", new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Left, GestureConfigContract.WheelDirections.Up, new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Left, GestureConfigContract.WheelDirections.Down, new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Right, GestureConfigContract.WheelDirections.Up, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Volume,
                Operation = GestureConfigContract.Operations.Increase,
                Amount = 2
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Right, GestureConfigContract.WheelDirections.Down, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Volume,
                Operation = GestureConfigContract.Operations.Decrease,
                Amount = 2
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Top, GestureConfigContract.WheelDirections.Up, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Brightness,
                Operation = GestureConfigContract.Operations.Increase
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Top, GestureConfigContract.WheelDirections.Down, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Brightness,
                Operation = GestureConfigContract.Operations.Decrease
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Bottom, GestureConfigContract.WheelDirections.Up, new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Bottom, GestureConfigContract.WheelDirections.Down, new GestureActionConfig())
        ];

        return config;
    }

    private static EdgeActionConfig CreateEdgeAction(
        string triggerType,
        string location,
        string wheelDirection,
        GestureActionConfig action)
    {
        return new EdgeActionConfig
        {
            Enabled = false,
            TriggerType = triggerType,
            Location = location,
            WheelDirection = wheelDirection,
            FrictionCount = 4,
            Action = action
        };
    }
}
