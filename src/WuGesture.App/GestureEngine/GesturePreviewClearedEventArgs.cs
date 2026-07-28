namespace WuGesture.App.GestureEngine;

public sealed class GesturePreviewClearedEventArgs(long sessionId) : EventArgs
{
    public long SessionId { get; } = sessionId;
}
