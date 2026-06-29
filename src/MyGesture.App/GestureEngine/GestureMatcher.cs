namespace MyGesture.App.GestureEngine;

public sealed class GestureMatcher
{
    private readonly List<GestureRule> rules;

    public GestureMatcher(IEnumerable<GestureRule> rules)
    {
        this.rules = rules.ToList();
    }

    public GestureRule? Match(IReadOnlyList<GestureDirection> pattern)
    {
        return rules.FirstOrDefault(rule => rule.Pattern.SequenceEqual(pattern));
    }

    public IReadOnlyList<GestureRule> Rules => rules;
}
