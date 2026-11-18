namespace WuGesture.App.GestureEngine;

/// <summary>
/// A configuration problem that prevents the application from starting safely. The message is written for
/// the user and shown before the process exits; the original configuration file is left untouched.
/// </summary>
public sealed class GestureConfigLoadException : Exception
{
    public GestureConfigLoadException(string message)
        : base(message)
    {
    }

    public GestureConfigLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
