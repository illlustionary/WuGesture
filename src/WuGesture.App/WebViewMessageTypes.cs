namespace WuGesture.App;

internal static class WebViewMessageTypes
{
    public const string GetStatus = "get-status";
    public const string SelectApplication = "select-application";
    public const string PickApplicationWindow = "pick-application-window";
    public const string StartGestureRecording = "start-gesture-recording";
    public const string StopGestureRecording = "stop-gesture-recording";
    public const string SetGesturePaused = "set-gesture-paused";
    public const string SetUserPaused = "set-user-paused";
    public const string StartHotkeyRecording = "start-hotkey-recording";
    public const string StopHotkeyRecording = "stop-hotkey-recording";
    public const string SaveRules = "save-rules";
    public const string WebDavTest = "webdav-test";
    public const string WebDavSave = "webdav-save";
    public const string WebDavRestore = "webdav-restore";
    public const string ExportConfig = "export-config";
    public const string ImportConfig = "import-config";
    public const string ReloadRules = "reload-rules";
    public const string ResetRules = "reset-rules";
    public const string PreviewLevelOsd = "preview-level-osd";
    public const string WindowMinimize = "window-minimize";
    public const string WindowToggleMaximize = "window-toggle-maximize";
    public const string WindowClose = "window-close";
    public const string WindowStartDrag = "window-start-drag";
    public const string WindowStartResize = "window-start-resize";

    public const string GestureRecorded = "gesture-recorded";
    public const string GestureActionFailed = "gesture-action-failed";
    public const string EdgeActionFailed = "edge-action-failed";
    public const string HotkeyRecorded = "hotkey-recorded";
    public const string Status = "status";
    public const string Rules = "rules";
    public const string ApplicationSelected = "application-selected";
    public const string ConfigResult = "config-result";
    public const string WebDavResult = "webdav-result";
    public const string WindowState = "window-state";
    public const string WindowResizeState = "window-resize-state";
}
