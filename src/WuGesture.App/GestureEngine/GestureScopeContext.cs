namespace WuGesture.App.GestureEngine;

public sealed record GestureScopeContext(string AppName, string CategoryName)
{
    public static GestureScopeContext Empty { get; } = new("", "");
}

public interface IGestureScopeContextProvider
{
    GestureScopeContext GetCurrentContext();
}
