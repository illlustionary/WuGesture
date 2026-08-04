namespace WuGesture.App;

internal static class AppIdentity
{
    public const string DisplayName = "WuGesture";
    public const string AppDataFolderName = "WuGesture";
    public const string StartupRegistryValueName = "WuGesture";
    public const string SingleInstanceMutexName = @"Local\WuGesture.SingleInstance";
    public const string ShowExistingInstanceEventName = @"Local\WuGesture.ShowExistingInstance";
    public const string ShowExistingInstanceCompletedEventName = @"Local\WuGesture.ShowExistingInstanceCompleted";
    public const string ElevatedRelaunchArgument = "--elevated-relaunch";
    public const string StartupLaunchArgument = "--startup";

    public static string GetLaunchExecutablePath()
    {
        return Application.ExecutablePath;
    }

    public static string GetDisplayVersion()
    {
        var version = typeof(AppIdentity).Assembly.GetName().Version;
        if (version is null)
        {
            return "v0.0.0";
        }

        return version.Build >= 0
            ? $"v{version.Major}.{version.Minor}.{version.Build}"
            : $"v{version.Major}.{version.Minor}";
    }
}
