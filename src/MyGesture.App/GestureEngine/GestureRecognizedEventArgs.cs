namespace MyGesture.App.GestureEngine;

public sealed class GestureRecognizedEventArgs : EventArgs
{
    public GestureRecognizedEventArgs(IReadOnlyList<GestureDirection> pattern, string actionName)
    {
        Pattern = pattern;
        ActionName = actionName;
    }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public string ActionName { get; }
}
