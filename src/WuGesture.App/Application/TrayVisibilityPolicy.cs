namespace WuGesture.App;

/// <summary>
/// Decides when the configuration window must stay hidden in the notification area.
/// <para>
/// A close request hides the form synchronously and releases the WebView2 host. The window is then
/// intentionally invisible until the tray restore path rebuilds it. Alt+Tab and Task View may still hold
/// a snapshot of the windows that were visible when they opened, so releasing the switcher can ask the
/// shell to show the form again. That show must be refused, otherwise the user sees an empty window whose
/// WebView2 host has already been disposed.
/// </para>
/// </summary>
internal static class TrayVisibilityPolicy
{
    public static bool ShouldHideToTray(bool alreadyHidden, bool isClosing)
        => !alreadyHidden && !isClosing;

    public static bool ShouldKeepHidden(bool isHiddenToTray, bool isRestoring, bool isClosing)
        => isHiddenToTray && !isRestoring && !isClosing;
}
