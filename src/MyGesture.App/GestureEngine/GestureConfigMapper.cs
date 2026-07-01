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
                Scope = NormalizeScope(rule.Scope),
                MouseButton = rule.MouseButton == GestureMouseButton.Middle ? "middle" : "right",
                ActionName = rule.ActionName,
                Pattern = rule.Pattern.Select(direction => direction.ToString()).ToList(),
                Action = ToConfigAction(rule.Action)
            }).ToList()
        };
    }

    private static GestureRule? ToRule(GestureRuleConfig config)
    {
        var pattern = new List<GestureDirection>();
        foreach (var directionName in config.Pattern)
        {
            if (!Enum.TryParse<GestureDirection>(directionName, ignoreCase: true, out var direction))
            {
                return null;
            }

            pattern.Add(direction);
        }

        if (pattern.Count == 0)
        {
            return null;
        }

        var actionType = config.Action.Type.Trim();
        if (string.Equals(actionType, "hotkey", StringComparison.OrdinalIgnoreCase))
        {
            return ToHotkeyRule(config, pattern);
        }

        if (string.Equals(actionType, "window", StringComparison.OrdinalIgnoreCase))
        {
            return ToWindowRule(config, pattern);
        }

        return null;
    }

    private static GestureRule? ToHotkeyRule(GestureRuleConfig config, IReadOnlyList<GestureDirection> pattern)
    {
        var keys = new List<Keys>();
        foreach (var keyName in config.Action.Keys)
        {
            if (!TryParseKey(keyName, out var key))
            {
                return null;
            }

            keys.Add(key);
        }

        if (keys.Count == 0)
        {
            return null;
        }

        return new GestureRule(
            pattern,
            NormalizeScope(config.Scope),
            string.IsNullOrWhiteSpace(config.ActionName) ? string.Join(" + ", config.Action.Keys) : config.ActionName,
            new HotkeyAction(keys),
            ParseMouseButton(config.MouseButton));
    }

    private static GestureRule? ToWindowRule(GestureRuleConfig config, IReadOnlyList<GestureDirection> pattern)
    {
        if (!TryParseWindowOperation(config.Action.Operation, out var operation))
        {
            return null;
        }

        return new GestureRule(
            pattern,
            NormalizeScope(config.Scope),
            string.IsNullOrWhiteSpace(config.ActionName) ? ToConfigOperationName(operation) : config.ActionName,
            new WindowControlAction(operation),
            ParseMouseButton(config.MouseButton));
    }

    private static GestureActionConfig ToConfigAction(GestureAction action)
    {
        return action switch
        {
            HotkeyAction hotkey => new GestureActionConfig
            {
                Type = "hotkey",
                Keys = hotkey.Keys.Select(ToConfigKeyName).ToList()
            },
            WindowControlAction window => new GestureActionConfig
            {
                Type = "window",
                Operation = ToConfigOperationName(window.Operation)
            },
            _ => new GestureActionConfig()
        };
    }

    private static string NormalizeScope(string scope)
    {
        return string.IsNullOrWhiteSpace(scope) ? "global" : scope.Trim();
    }

    private static GestureMouseButton ParseMouseButton(string? mouseButton)
    {
        return string.Equals(mouseButton, "middle", StringComparison.OrdinalIgnoreCase)
            ? GestureMouseButton.Middle
            : GestureMouseButton.Right;
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

    private static bool TryParseWindowOperation(string value, out WindowControlOperation operation)
    {
        operation = WindowControlOperation.ToggleMaximize;
        var normalized = value.Trim().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        if (normalized.Equals("toggletopmost", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("topmost", StringComparison.OrdinalIgnoreCase))
        {
            operation = WindowControlOperation.ToggleTopMost;
            return true;
        }

        if (normalized.Equals("togglemaximize", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("maximize", StringComparison.OrdinalIgnoreCase))
        {
            operation = WindowControlOperation.ToggleMaximize;
            return true;
        }

        if (normalized.Equals("minimize", StringComparison.OrdinalIgnoreCase))
        {
            operation = WindowControlOperation.Minimize;
            return true;
        }

        if (normalized.Equals("close", StringComparison.OrdinalIgnoreCase))
        {
            operation = WindowControlOperation.Close;
            return true;
        }

        return false;
    }

    private static string ToConfigOperationName(WindowControlOperation operation)
    {
        return operation switch
        {
            WindowControlOperation.ToggleTopMost => "toggle-topmost",
            WindowControlOperation.ToggleMaximize => "toggle-maximize",
            WindowControlOperation.Minimize => "minimize",
            WindowControlOperation.Close => "close",
            _ => "toggle-maximize"
        };
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
