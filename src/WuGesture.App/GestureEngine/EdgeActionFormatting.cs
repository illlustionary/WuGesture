namespace WuGesture.App.GestureEngine;

internal static class EdgeActionLocationMapper
{
    public static string ToConfigLocation(EdgeLocation location)
    {
        return location switch
        {
            EdgeLocation.TopLeft => GestureConfigContract.EdgeLocations.TopLeft,
            EdgeLocation.TopRight => GestureConfigContract.EdgeLocations.TopRight,
            EdgeLocation.BottomLeft => GestureConfigContract.EdgeLocations.BottomLeft,
            EdgeLocation.BottomRight => GestureConfigContract.EdgeLocations.BottomRight,
            EdgeLocation.Left => GestureConfigContract.EdgeLocations.Left,
            EdgeLocation.Right => GestureConfigContract.EdgeLocations.Right,
            EdgeLocation.Top => GestureConfigContract.EdgeLocations.Top,
            EdgeLocation.Bottom => GestureConfigContract.EdgeLocations.Bottom,
            _ => ""
        };
    }
}

internal static class EdgeActionNameFormatter
{
    public static string Format(EdgeActionConfig config)
    {
        var triggerLabel = (config.TriggerType ?? "").Trim().ToLowerInvariant() switch
        {
            GestureConfigContract.EdgeTriggerTypes.Corner => "触发角",
            GestureConfigContract.EdgeTriggerTypes.Friction => "摩擦边",
            GestureConfigContract.EdgeTriggerTypes.Wheel => "边缘滚动",
            _ => "边缘操作"
        };
        var locationLabel = (config.Location ?? "").Trim().ToLowerInvariant() switch
        {
            GestureConfigContract.EdgeLocations.TopLeft => "左上角",
            GestureConfigContract.EdgeLocations.TopRight => "右上角",
            GestureConfigContract.EdgeLocations.BottomLeft => "左下角",
            GestureConfigContract.EdgeLocations.BottomRight => "右下角",
            GestureConfigContract.EdgeLocations.Left => "左边",
            GestureConfigContract.EdgeLocations.Right => "右边",
            GestureConfigContract.EdgeLocations.Top => "上边",
            GestureConfigContract.EdgeLocations.Bottom => "下边",
            _ => ""
        };
        var wheelLabel = string.Equals(config.WheelDirection, GestureConfigContract.WheelDirections.Down, StringComparison.OrdinalIgnoreCase)
            ? "滚轮下"
            : string.Equals(config.WheelDirection, GestureConfigContract.WheelDirections.Up, StringComparison.OrdinalIgnoreCase)
                ? "滚轮上"
                : "";

        return string.Join(' ', new[] { triggerLabel, locationLabel, wheelLabel }.Where(static part => part.Length > 0));
    }
}
