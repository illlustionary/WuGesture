using System.Windows.Forms;

namespace WuGesture.App;

/// <summary>
/// Decides whether a <see cref="FormClosingEventArgs.CloseReason"/> is a close request coming from
/// outside the app, which must honour the configured close-button behavior instead of terminating.
/// <para>
/// WinForms reports the title-bar close button and Alt+F4 as <see cref="CloseReason.UserClosing"/>,
/// but any other <c>WM_CLOSE</c> — the "close window" mouse gesture, automation tools, Task Manager —
/// arrives as <see cref="CloseReason.TaskManagerClosing"/> because the form never saw the matching
/// <c>SC_CLOSE</c> system command. System-driven closes keep their own reasons
/// (<see cref="CloseReason.WindowsShutDown"/>, <see cref="CloseReason.ApplicationExitCall"/>,
/// <see cref="CloseReason.MdiFormClosing"/>, <see cref="CloseReason.FormOwnerClosing"/>) and still exit.
/// </para>
/// </summary>
internal static class CloseRequestPolicy
{
    public static bool IsCloseRequest(CloseReason reason)
        => reason is CloseReason.UserClosing or CloseReason.TaskManagerClosing;
}
