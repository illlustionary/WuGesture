namespace WuGesture.App;

internal static class AppIdentity
{
    public const string DisplayName = "WuGesture";
    public const string AppDataFolderName = "WuGesture";
    public const string StartupRegistryValueName = "WuGesture";
    public const string SingleInstanceMutexName = @"Local\WuGesture.SingleInstance";
    public const string ShowExistingInstanceEventName = @"Local\WuGesture.ShowExistingInstance";
    public const string ShowExistingInstanceCompletedEventName = @"Local\WuGesture.ShowExistingInstanceCompleted";
    public const string ExitRequestedEventName = @"Local\WuGesture.ExitRequested";
    public const string ExitRequestedCompletedEventName = @"Local\WuGesture.ExitRequestedCompleted";
    public const string ElevatedRelaunchArgument = "--elevated-relaunch";
    public const string StartupLaunchArgument = "--startup";
    // Added by the updater when it restarts the freshly installed version.
    public const string UpdateRestartArgument = "--updated";
    // Closes the running instance without a UI or elevation prompt.
    public const string ExitArgument = "--exit";
    // Set as the scheduled task action: the instance knows it was started with an elevated token.
    public const string ScheduledLaunchArgument = "--scheduled-launch";
    // Elevated helper modes that register or remove the scheduled task, then exit.
    public const string CreateScheduledTaskArgument = "--task-create";
    public const string DeleteScheduledTaskArgument = "--task-delete";
    // Hidden helper that waits for a process to exit and then runs the scheduled task.
    public const string TaskHandoverArgument = "--task-handover";

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
