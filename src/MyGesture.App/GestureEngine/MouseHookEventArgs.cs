using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class MouseHookEventArgs : EventArgs
{
    public MouseHookEventArgs(Point location)
    {
        Location = location;
    }

    public Point Location { get; }

    public bool Handled { get; set; }
}
