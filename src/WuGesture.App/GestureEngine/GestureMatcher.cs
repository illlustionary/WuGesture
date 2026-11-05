namespace WuGesture.App.GestureEngine;

public sealed class GestureMatcher
{
    private readonly List<GestureRule> rules;

    public GestureMatcher(IEnumerable<GestureRule> rules)
    {
        this.rules = rules.ToList();
    }

    public GestureRule? Match(
        IReadOnlyList<GestureDirection> pattern,
        GestureScopeContext? context = null,
        GestureMouseButton button = GestureMouseButton.Right)
    {
        context ??= GestureScopeContext.Empty;

        GestureRule? bestMatch = null;
        var bestPriority = int.MinValue;

        foreach (var rule in rules)
        {
            if (rule.MouseButton != button)
            {
                continue;
            }

            // A rule with a wheel modifier is only reachable while that modifier is active.
            if (rule.WheelModifier is not null)
            {
                continue;
            }

            if (!rule.Pattern.SequenceEqual(pattern))
            {
                continue;
            }

            if (!TryGetScopePriority(rule.Scope, context, out var priority))
            {
                continue;
            }

            if (priority > bestPriority)
            {
                bestPriority = priority;
                bestMatch = rule;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Matches the current direction pattern with an active wheel modifier. The wheel direction
    /// is part of the gesture identity, so a rule only matches the exact pattern and modifier.
    /// </summary>
    public GestureRule? MatchWheel(
        GestureWheelDirection direction,
        IReadOnlyList<GestureDirection> pattern,
        GestureScopeContext? context = null,
        GestureMouseButton button = GestureMouseButton.Right)
    {
        context ??= GestureScopeContext.Empty;

        GestureRule? bestMatch = null;
        var bestPriority = int.MinValue;

        foreach (var rule in rules)
        {
            if (rule.MouseButton != button ||
                rule.WheelModifier != direction ||
                !rule.Pattern.SequenceEqual(pattern))
            {
                continue;
            }

            if (!TryGetScopePriority(rule.Scope, context, out var priority))
            {
                continue;
            }

            if (priority > bestPriority)
            {
                bestPriority = priority;
                bestMatch = rule;
            }
        }

        return bestMatch;
    }

    public IReadOnlyList<GestureRule> Rules => rules;

    /// <summary>
    /// Used to decide whether wheel input should be swallowed while the given button is held.
    /// Without any wheel-modifier rule the wheel must keep reaching the target application.
    /// </summary>
    public bool HasWheelRules(GestureMouseButton button)
    {
        return rules.Any(rule => rule.MouseButton == button && rule.WheelModifier is not null);
    }

    private static bool TryGetScopePriority(string scope, GestureScopeContext context, out int priority)
    {
        priority = int.MinValue;

        var normalizedScope = scope.Trim();
        if (normalizedScope.Length == 0 ||
            normalizedScope.Equals(GestureConfigContract.Scopes.Global, StringComparison.OrdinalIgnoreCase))
        {
            priority = 0;
            return true;
        }

        if (TryGetPrefixedScopeValue(normalizedScope, GestureConfigContract.Scopes.App, out var appScopeValue))
        {
            if (MatchesScopeValue(appScopeValue, context.AppName))
            {
                priority = int.MaxValue;
                return true;
            }

            return false;
        }

        if (TryGetPrefixedScopeValue(normalizedScope, GestureConfigContract.Scopes.Category, out var categoryScopeValue))
        {
            if (TryGetCategoryPriority(categoryScopeValue, context.CategoryNames, out priority))
            {
                return true;
            }

            return false;
        }

        if (MatchesScopeValue(normalizedScope, context.AppName))
        {
            priority = int.MaxValue;
            return true;
        }

        if (TryGetCategoryPriority(normalizedScope, context.CategoryNames, out priority))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetPrefixedScopeValue(string scope, string prefix, out string value)
    {
        value = "";
        var prefixWithSeparator = prefix + ":";
        if (!scope.StartsWith(prefixWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = scope[prefixWithSeparator.Length..].Trim();
        return value.Length > 0;
    }

    private static bool MatchesScopeValue(string scopeValue, string currentValue)
    {
        return !string.IsNullOrWhiteSpace(currentValue) &&
               scopeValue.Equals(currentValue.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetCategoryPriority(
        string scopeValue,
        IReadOnlyList<string> categoryNames,
        out int priority)
    {
        priority = int.MinValue;
        for (var index = 0; index < categoryNames.Count; index++)
        {
            if (MatchesScopeValue(scopeValue, categoryNames[index]))
            {
                priority = categoryNames.Count - index;
                return true;
            }
        }

        return false;
    }
}
