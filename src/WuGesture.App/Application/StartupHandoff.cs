namespace WuGesture.App;

[Flags]
internal enum StartupHandoffFlags
{
    None = 0,
    HiddenToTray = 1,
    UpdateRestart = 2
}

/// <summary>
/// Carries startup flags across the scheduled-task relaunch. The task action is a fixed command line,
/// so flags cannot travel as arguments; the relaunching instance leaves a marker and the task instance
/// consumes it. Only the initial window state and the update notification depend on this file.
/// </summary>
internal static class StartupHandoff
{
    private static string MarkerPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppIdentity.AppDataFolderName,
        "pending-startup");

    public static void Mark(StartupHandoffFlags flags)
    {
        if (flags == StartupHandoffFlags.None)
        {
            // Drop any marker left behind by an earlier attempt that never completed.
            Clear();
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(MarkerPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(MarkerPath, flags.ToString());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AppLogger.Warning(
                "StartupHandoff",
                "mark-failed",
                "无法记录启动标记，任务实例将显示配置窗口。",
                exception);
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(MarkerPath))
            {
                File.Delete(MarkerPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AppLogger.Warning("StartupHandoff", "clear-failed", "无法清理启动标记。", exception);
        }
    }

    public static StartupHandoffFlags Consume()
    {
        try
        {
            if (!File.Exists(MarkerPath))
            {
                return StartupHandoffFlags.None;
            }

            var content = File.ReadAllText(MarkerPath);
            File.Delete(MarkerPath);
            return Enum.TryParse<StartupHandoffFlags>(content, ignoreCase: true, out var flags)
                ? flags
                : StartupHandoffFlags.None;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AppLogger.Warning("StartupHandoff", "consume-failed", "无法读取启动标记。", exception);
            return StartupHandoffFlags.None;
        }
    }
}
