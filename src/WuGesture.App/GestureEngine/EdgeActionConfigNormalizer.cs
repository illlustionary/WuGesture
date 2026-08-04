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
        action.Action ??= new GestureActionConfig();
        if (string.Equals(action.Action.Type, GestureConfigContract.ActionTypes.Program, StringComparison.OrdinalIgnoreCase))
        {
            action.Action.Type = GestureConfigContract.ActionTypes.Hotkey;
            action.Action.Keys = [];
            action.Action.Path = "";
            action.Action.Arguments = [];
        }

        if (string.Equals(action.TriggerType, GestureConfigContract.EdgeTriggerTypes.Friction, StringComparison.OrdinalIgnoreCase))
        {
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

}
