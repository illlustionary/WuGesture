using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public static class GestureConfigMapper
{
    public static IReadOnlyList<GestureRule> ToRules(GestureConfig config)
    {
        return config.Rules
            .Select(ToRule)
            .Where(rule => rule is not null)
            .Cast<GestureRule>()
            .ToArray();
    }

    public static GestureConfig FromRules(IEnumerable<GestureRule> rules)
    {
        return new GestureConfig
        {
            Rules = rules.Select(rule => new GestureRuleConfig
            {
                Scope = rule.Scope,
                ActionName = rule.ActionName,
                Pattern = rule.Pattern.Select(direction => direction.ToString()).ToList(),
                Action = new GestureActionConfig
                {
                    Type = "hotkey",
                    Keys = rule.Action.Keys.Select(ToConfigKeyName).ToList()
                }
            }).ToList()
        };
    }

    private static GestureRule? ToRule(GestureRuleConfig config)
    {
        if (!string.Equals(config.Action.Type, "hotkey", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var pattern = new List<GestureDirection>();
        foreach (var directionName in config.Pattern)
        {
            if (!Enum.TryParse<GestureDirection>(directionName, ignoreCase: true, out var direction))
            {
                return null;
            }

            pattern.Add(direction);
        }

        var keys = new List<Keys>();
        foreach (var keyName in config.Action.Keys)
        {
            if (!TryParseKey(keyName, out var key))
            {
                return null;
            }

            keys.Add(key);
        }

        if (pattern.Count == 0 || keys.Count == 0)
        {
            return null;
        }

        return new GestureRule(
            pattern,
            string.IsNullOrWhiteSpace(config.Scope) ? "global" : config.Scope,
            string.IsNullOrWhiteSpace(config.ActionName) ? string.Join(" + ", config.Action.Keys) : config.ActionName,
            new HotkeyAction(keys));
    }

    private static bool TryParseKey(string value, out Keys key)
    {
        key = Keys.None;
        var normalized = value.Trim();

        if (normalized.Equals("Alt", StringComparison.OrdinalIgnoreCase))
        {
            key = Keys.Menu;
            return true;
        }

        if (normalized.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Control", StringComparison.OrdinalIgnoreCase))
        {
            key = Keys.ControlKey;
            return true;
        }

        if (normalized.Equals("Shift", StringComparison.OrdinalIgnoreCase))
        {
            key = Keys.ShiftKey;
            return true;
        }

        if (normalized.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Windows", StringComparison.OrdinalIgnoreCase))
        {
            key = Keys.LWin;
            return true;
        }

        return Enum.TryParse(normalized, ignoreCase: true, out key);
    }

    private static string ToConfigKeyName(Keys key)
    {
        return key switch
        {
            Keys.Menu => "Alt",
            Keys.ControlKey => "Control",
            Keys.ShiftKey => "Shift",
            Keys.LWin => "Win",
            _ => key.ToString()
        };
    }
}
