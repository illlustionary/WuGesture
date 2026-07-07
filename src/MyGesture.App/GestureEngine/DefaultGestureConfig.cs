namespace MyGesture.App.GestureEngine;

public static class DefaultGestureConfig
{
    public static GestureConfig Create()
    {
        var config = GestureConfigMapper.FromRules(DefaultGestureRules.Create());
        config.EdgeActions =
        [
            CreateEdgeAction("corner", "top-left", "", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction("corner", "top-right", "", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction("corner", "bottom-left", "", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win"]
            }),
            CreateEdgeAction("corner", "bottom-right", "", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win", "D"]
            }),
            CreateEdgeAction("friction", "left", "", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Control", "Shift", "Escape"]
            }),
            CreateEdgeAction("friction", "right", "", new GestureActionConfig()),
            CreateEdgeAction("friction", "top", "", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["M", "I", "K", "U"]
            }),
            CreateEdgeAction("friction", "bottom", "", new GestureActionConfig()),
            CreateEdgeAction("wheel", "left", "up", new GestureActionConfig()),
            CreateEdgeAction("wheel", "left", "down", new GestureActionConfig()),
            CreateEdgeAction("wheel", "right", "up", new GestureActionConfig
            {
                Type = "volume",
                Operation = "increase",
                Amount = 2
            }),
            CreateEdgeAction("wheel", "right", "down", new GestureActionConfig
            {
                Type = "volume",
                Operation = "decrease",
                Amount = 2
            }),
            CreateEdgeAction("wheel", "top", "up", new GestureActionConfig
            {
                Type = "brightness",
                Operation = "increase"
            }),
            CreateEdgeAction("wheel", "top", "down", new GestureActionConfig
            {
                Type = "brightness",
                Operation = "decrease"
            }),
            CreateEdgeAction("wheel", "bottom", "up", new GestureActionConfig()),
            CreateEdgeAction("wheel", "bottom", "down", new GestureActionConfig())
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
