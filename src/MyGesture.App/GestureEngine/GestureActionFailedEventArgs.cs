namespace MyGesture.App.GestureEngine;

public sealed class GestureActionFailedEventArgs : EventArgs
{
    public GestureActionFailedEventArgs(IReadOnlyList<GestureDirection> pattern, string actionName, Exception exception)
    {
        Pattern = pattern;
        ActionName = actionName;
        Exception = exception;
    }

    public IReadOnlyList<GestureDirection> Pattern { get; }

    public string ActionName { get; }

    public Exception Exception { get; }
}
