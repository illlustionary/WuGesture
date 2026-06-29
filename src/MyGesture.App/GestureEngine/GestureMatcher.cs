using System.Windows.Forms;

namespace MyGesture.App.GestureEngine;

public sealed class GestureMatcher
{
    private readonly List<GestureRule> rules =
    [
        new([GestureDirection.Left], "Back", new HotkeyAction([Keys.Menu, Keys.Left])),
        new([GestureDirection.Right], "Forward", new HotkeyAction([Keys.Menu, Keys.Right])),
        new([GestureDirection.Down, GestureDirection.Right], "Close Tab", new HotkeyAction([Keys.ControlKey, Keys.W]))
    ];

    public GestureRule? Match(IReadOnlyList<GestureDirection> pattern)
    {
        return rules.FirstOrDefault(rule => rule.Pattern.SequenceEqual(pattern));
    }

    public IReadOnlyList<GestureRule> Rules => rules;
}
