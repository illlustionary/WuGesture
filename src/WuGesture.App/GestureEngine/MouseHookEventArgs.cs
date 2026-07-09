using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class MouseHookEventArgs : EventArgs
{
    public MouseHookEventArgs(Point location)
    {
        Location = location;
    }

    public Point Location { get; }

    public bool Handled { get; set; }
}

public sealed class MouseWheelHookEventArgs : EventArgs
{
    public MouseWheelHookEventArgs(Point location, int delta)
    {
        Location = location;
        Delta = delta;
    }

    public Point Location { get; }

    public int Delta { get; }

    public bool Handled { get; set; }
}
