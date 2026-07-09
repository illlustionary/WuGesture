namespace WuGesture.App;

internal static class AppIdentity
{
    public const string DisplayName = "WuGesture";
    public const string AppDataFolderName = "WuGesture";
    public const string StartupRegistryValueName = "WuGesture";
    public const string SingleInstanceMutexName = @"Local\WuGesture.SingleInstance";
    public const string ShowExistingInstanceEventName = @"Local\WuGesture.ShowExistingInstance";
    public const string ElevatedRelaunchArgument = "--elevated-relaunch";
    public const string StartupLaunchArgument = "--startup";
}
