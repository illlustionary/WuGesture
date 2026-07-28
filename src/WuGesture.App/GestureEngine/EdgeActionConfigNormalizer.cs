namespace WuGesture.App.GestureEngine;

internal static class EdgeActionConfigNormalizer
{
    public static IReadOnlyList<EdgeActionConfig> Normalize(IEnumerable<EdgeActionConfig>? source)
    {
        return (source ?? [])
            .Where(action => !string.IsNullOrWhiteSpace(action.TriggerType) && !string.IsNullOrWhiteSpace(action.Location))
            .Select(NormalizeAction)
            .ToArray();
    }

    private static EdgeActionConfig NormalizeAction(EdgeActionConfig action)
    {
        if (string.Equals(action.TriggerType, GestureConfigContract.EdgeTriggerTypes.Friction, StringComparison.OrdinalIgnoreCase))
        {
            action.Location = MigrateLegacyFrictionLocation(action.Location);
            if (!IsEdgeLocation(action.Location))
            {
                action.Location = GestureConfigContract.EdgeLocations.Left;
            }
        }
        else if (string.Equals(action.TriggerType, GestureConfigContract.EdgeTriggerTypes.Wheel, StringComparison.OrdinalIgnoreCase) &&
            !IsEdgeLocation(action.Location))
        {
            action.Location = GestureConfigContract.EdgeLocations.Left;
        }

        return action;
    }

    private static bool IsEdgeLocation(string location)
    {
        return location.Trim().ToLowerInvariant() is
            GestureConfigContract.EdgeLocations.Left or
            GestureConfigContract.EdgeLocations.Right or
            GestureConfigContract.EdgeLocations.Top or
            GestureConfigContract.EdgeLocations.Bottom;
    }

    private static string MigrateLegacyFrictionLocation(string location)
    {
        return location.Trim().ToLowerInvariant() switch
        {
            GestureConfigContract.EdgeLocations.TopLeft => GestureConfigContract.EdgeLocations.Left,
            GestureConfigContract.EdgeLocations.TopRight => GestureConfigContract.EdgeLocations.Top,
            GestureConfigContract.EdgeLocations.BottomLeft => GestureConfigContract.EdgeLocations.Bottom,
            GestureConfigContract.EdgeLocations.BottomRight => GestureConfigContract.EdgeLocations.Right,
            _ => location
        };
    }
}
