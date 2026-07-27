namespace WuGesture.App.GestureEngine;

public sealed record GestureScopeContext(string AppName, IReadOnlyList<string> CategoryNames)
{
    public static GestureScopeContext Empty { get; } = new("", []);
}

public interface IGestureScopeContextProvider
{
    GestureScopeContext GetCurrentContext();

    GestureScopeContext GetContextForWindow(IntPtr window);
}
