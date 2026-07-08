namespace MyGesture.App;

internal static class AppIdentity
{
    public const string DisplayName = "Wu Gesture";
    public const string AppDataFolderName = "MyGesture";
    public const string StartupRegistryValueName = "WuGesture";
    public const string LegacyStartupRegistryValueName = "MyGesture";
    public const string SingleInstanceMutexName = @"Local\WuGesture.SingleInstance";
    public const string ShowExistingInstanceEventName = @"Local\WuGesture.ShowExistingInstance";
    public const string ElevatedRelaunchArgument = "--elevated-relaunch";
    public const string StartupLaunchArgument = "--startup";
}
