namespace MyGesture.App.GestureEngine;

public static class DefaultGestureConfig
{
    public static GestureConfig Create()
    {
        var config = GestureConfigMapper.FromRules(DefaultGestureRules.Create());
        config.EdgeActions =
        [
            CreateEdgeAction("corner", "top-left", "", "触发角 左上角", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction("corner", "top-right", "", "触发角 右上角", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction("corner", "bottom-left", "", "触发角 左下角", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win"]
            }),
            CreateEdgeAction("corner", "bottom-right", "", "触发角 右下角", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Win", "D"]
            }),
            CreateEdgeAction("friction", "left", "", "左", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["Control", "Shift", "Escape"]
            }),
            CreateEdgeAction("friction", "right", "", "摩擦边 右下角", new GestureActionConfig()),
            CreateEdgeAction("friction", "top", "", "摩擦边 右上角", new GestureActionConfig
            {
                Type = "hotkey",
                Keys = ["M", "I", "K", "U"]
            }),
            CreateEdgeAction("friction", "bottom", "", "摩擦边 左下角", new GestureActionConfig()),
            CreateEdgeAction("wheel", "left", "up", "滚动边 左边 滚轮上", new GestureActionConfig()),
            CreateEdgeAction("wheel", "left", "down", "滚动边 左边 滚轮下", new GestureActionConfig()),
            CreateEdgeAction("wheel", "right", "up", "滚动边 右边 滚轮上", new GestureActionConfig
            {
                Type = "volume",
                Operation = "increase",
                Amount = 2
            }),
            CreateEdgeAction("wheel", "right", "down", "滚动边 右边 滚轮下", new GestureActionConfig
            {
                Type = "volume",
                Operation = "decrease",
                Amount = 2
            }),
            CreateEdgeAction("wheel", "top", "up", "滚动边 上边 滚轮上", new GestureActionConfig
            {
                Type = "brightness",
                Operation = "increase"
            }),
            CreateEdgeAction("wheel", "top", "down", "滚动边 上边 滚轮下", new GestureActionConfig
            {
                Type = "brightness",
                Operation = "decrease"
            }),
            CreateEdgeAction("wheel", "bottom", "up", "滚动边 下边 滚轮上", new GestureActionConfig()),
            CreateEdgeAction("wheel", "bottom", "down", "滚动边 下边 滚轮下", new GestureActionConfig())
        ];

        return config;
    }

    private static EdgeActionConfig CreateEdgeAction(
        string triggerType,
        string location,
        string wheelDirection,
        string actionName,
        GestureActionConfig action)
    {
        return new EdgeActionConfig
        {
            Enabled = false,
            TriggerType = triggerType,
            Location = location,
            WheelDirection = wheelDirection,
            FrictionCount = 4,
            ActionName = actionName,
            Action = action
        };
    }
}
