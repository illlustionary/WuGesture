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
            UiSettings = new GestureUiSettings(),
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

        var action = ToAction(config.Action);
        if (action is null)
        {
            return null;
        }

        return new GestureRule(
            pattern,
            NormalizeScope(config.Scope),
            string.IsNullOrWhiteSpace(config.ActionName) ? GetDefaultActionName(config.Action, action) : config.ActionName,
            action,
            ParseMouseButton(config.MouseButton));
    }

    public static GestureAction? ToAction(GestureActionConfig config)
    {
        var actionType = config.Type.Trim();
        if (string.Equals(actionType, "hotkey", StringComparison.OrdinalIgnoreCase))
        {
            return ToHotkeyAction(config);
        }

        if (string.Equals(actionType, "window", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseWindowOperation(config.Operation, out var operation)
                ? new WindowControlAction(operation)
                : null;
        }

        if (string.Equals(actionType, "volume", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseVolumeOperation(config.Operation, out var operation)
                ? new VolumeControlAction(operation, NormalizeAmount(config.Amount))
                : null;
        }

        if (string.Equals(actionType, "brightness", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseBrightnessOperation(config.Operation, out var operation)
                ? new BrightnessControlAction(operation, NormalizeAmount(config.Amount))
                : null;
        }

        return null;
    }

    public static GestureActionConfig ToConfigAction(GestureAction action)
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
            VolumeControlAction volume => new GestureActionConfig
            {
                Type = "volume",
                Operation = ToConfigOperationName(volume.Operation),
                Amount = NormalizeAmount(volume.Amount)
            },
            BrightnessControlAction brightness => new GestureActionConfig
            {
                Type = "brightness",
                Operation = ToConfigOperationName(brightness.Operation),
                Amount = NormalizeAmount(brightness.Amount)
            },
            _ => new GestureActionConfig()
        };
    }

    private static GestureAction? ToHotkeyAction(GestureActionConfig config)
    {
        var keys = new List<Keys>();
        foreach (var keyName in config.Keys)
        {
            if (!TryParseKey(keyName, out var key))
            {
                return null;
            }

            keys.Add(key);
        }

        return keys.Count == 0 ? null : new HotkeyAction(keys);
    }

    private static string GetDefaultActionName(GestureActionConfig config, GestureAction action)
    {
        return action switch
        {
            HotkeyAction => string.Join(" + ", config.Keys),
            WindowControlAction window => ToConfigOperationName(window.Operation),
            VolumeControlAction volume => $"volume-{ToConfigOperationName(volume.Operation)}",
            BrightnessControlAction brightness => $"brightness-{ToConfigOperationName(brightness.Operation)}",
            _ => ""
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

    private static bool TryParseVolumeOperation(string value, out VolumeControlOperation operation)
    {
        operation = VolumeControlOperation.Increase;
        var normalized = value.Trim().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        if (normalized.Equals("increase", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("up", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("plus", StringComparison.OrdinalIgnoreCase))
        {
            operation = VolumeControlOperation.Increase;
            return true;
        }

        if (normalized.Equals("decrease", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("down", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("minus", StringComparison.OrdinalIgnoreCase))
        {
            operation = VolumeControlOperation.Decrease;
            return true;
        }

        if (normalized.Equals("mute", StringComparison.OrdinalIgnoreCase))
        {
            operation = VolumeControlOperation.Mute;
            return true;
        }

        return false;
    }

    private static bool TryParseBrightnessOperation(string value, out BrightnessControlOperation operation)
    {
        operation = BrightnessControlOperation.Increase;
        var normalized = value.Trim().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        if (normalized.Equals("increase", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("up", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("plus", StringComparison.OrdinalIgnoreCase))
        {
            operation = BrightnessControlOperation.Increase;
            return true;
        }

        if (normalized.Equals("decrease", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("down", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("minus", StringComparison.OrdinalIgnoreCase))
        {
            operation = BrightnessControlOperation.Decrease;
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

    private static string ToConfigOperationName(VolumeControlOperation operation)
    {
        return operation switch
        {
            VolumeControlOperation.Increase => "increase",
            VolumeControlOperation.Decrease => "decrease",
            VolumeControlOperation.Mute => "mute",
            _ => "increase"
        };
    }

    private static string ToConfigOperationName(BrightnessControlOperation operation)
    {
        return operation switch
        {
            BrightnessControlOperation.Increase => "increase",
            BrightnessControlOperation.Decrease => "decrease",
            _ => "increase"
        };
    }

    private static int NormalizeAmount(int amount)
    {
        return Math.Min(100, Math.Max(1, amount <= 0 ? 5 : amount));
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
