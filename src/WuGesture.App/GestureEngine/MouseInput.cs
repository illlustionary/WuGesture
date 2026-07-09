using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

public static class MouseInput
{
    private const uint MouseEventFRightDown = 0x0008;
    private const uint MouseEventFRightUp = 0x0010;
    private const uint MouseEventFMiddleDown = 0x0020;
    private const uint MouseEventFMiddleUp = 0x0040;

    public static void ReplayRightClick()
    {
        mouse_event(MouseEventFRightDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseEventFRightUp, 0, 0, 0, UIntPtr.Zero);
    }

    public static void ReplayMiddleClick()
    {
        mouse_event(MouseEventFMiddleDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseEventFMiddleUp, 0, 0, 0, UIntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(
        uint dwFlags,
        uint dx,
        uint dy,
        uint dwData,
        UIntPtr dwExtraInfo);
}
