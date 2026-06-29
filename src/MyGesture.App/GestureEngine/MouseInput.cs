using System.Runtime.InteropServices;

namespace MyGesture.App.GestureEngine;

public static class MouseInput
{
    private const uint MouseEventFRightDown = 0x0008;
    private const uint MouseEventFRightUp = 0x0010;

    public static void ReplayRightClick()
    {
        mouse_event(MouseEventFRightDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseEventFRightUp, 0, 0, 0, UIntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(
        uint dwFlags,
        uint dx,
        uint dy,
        uint dwData,
        UIntPtr dwExtraInfo);
}
