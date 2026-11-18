using System.ComponentModel;
using System.Diagnostics;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

/// <summary>
/// Starts an elevated instance when the configured start mode requires it: either through the UAC
/// prompt or through the "highest privileges" scheduled task that skips it.
/// The owning form only decides what "close" means for it.
/// </summary>
internal sealed class ElevationLauncher
{
    private const int Win32ErrorCancelled = 1223;
    private readonly bool startHiddenToTray;
    private readonly bool suppressDialogs;
    private readonly bool isUpdateRestart;
    private readonly bool isScheduledLaunch;
    private readonly Action requestClose;

    public ElevationLauncher(
        bool startHiddenToTray,
        bool suppressDialogs,
        bool isUpdateRestart,
        bool isScheduledLaunch,
        Action requestClose)
    {
        this.startHiddenToTray = startHiddenToTray;
        this.suppressDialogs = suppressDialogs;
        this.isUpdateRestart = isUpdateRestart;
        this.isScheduledLaunch = isScheduledLaunch;
        this.requestClose = requestClose;
    }

    /// <summary>
    /// Returns true when the current process must stop because an elevated instance took over, or because
    /// elevation was refused and running without it would leave the gesture service with partial rights.
    /// </summary>
    public bool TryRelaunch(AppBehaviorUiSettings settings)
    {
        var startMode = settings.StartMode;
        var isElevated = ElevationState.IsRunningAsAdministrator();

        if (startMode == GestureConfigContract.AppStartModes.SkipUac)
        {
            if (isElevated)
            {
                return false;
            }

            // HighestAvailable cannot elevate accounts that are not in the administrators group. In that
            // case the task instance keeps starting without a matching token, so starting the handover
            // again would loop forever; run with the current rights and say why.
            if (isScheduledLaunch)
            {
                AppLogger.Warning(
                    "ElevationLauncher",
                    "scheduled-task-not-elevated",
                    "The scheduled task started WuGesture without an administrator token.");
                ShowWarning(
                    "计划任务已启动 WuGesture，但当前账户无法获得管理员权限（可能不在管理员组，或被组策略限制）。\n\n" +
                    "本次将以当前权限继续运行。请在设置中改用“管理员启动”或“普通启动”。");
                return false;
            }

            return RelaunchThroughScheduledTask();
        }

        if (isElevated)
        {
            // A task left over from a previous "skip UAC" session could still launch this executable
            // with administrator rights; removing it is free while we already hold that token.
            RemoveScheduledTaskIfPresent();
            return false;
        }

        return startMode == GestureConfigContract.AppStartModes.Admin && RelaunchWithUac();
    }

    private bool RelaunchThroughScheduledTask()
    {
        string failure;
        try
        {
            if (!ScheduledTaskLauncher.EnsureRegistered(out failure))
            {
                return AbortScheduledTaskRelaunch(failure);
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error(
                "ElevationLauncher",
                "scheduled-task-unavailable",
                "Unable to register the elevated scheduled task.",
                exception);
            return AbortScheduledTaskRelaunch(exception.Message);
        }

        var flags = StartupHandoffFlags.None;
        if (startHiddenToTray)
        {
            flags |= StartupHandoffFlags.HiddenToTray;
        }

        if (isUpdateRestart)
        {
            flags |= StartupHandoffFlags.UpdateRestart;
        }

        StartupHandoff.Mark(flags);
        if (!ScheduledTaskLauncher.StartHandover(Environment.ProcessId))
        {
            StartupHandoff.Clear();
            return AbortScheduledTaskRelaunch("无法启动计划任务交接进程。");
        }

        AppLogger.Information(
            "ElevationLauncher",
            "scheduled-task-relaunch",
            "Restarting the app through the elevated scheduled task.");
        requestClose();
        return true;
    }

    private bool AbortScheduledTaskRelaunch(string failure)
    {
        AppLogger.Warning("ElevationLauncher", "scheduled-task-unavailable", failure);
        ShowWarning(
            $"无法启用“跳过 UAC 管理员启动”：{failure}\n\n" +
            "本次将以普通权限继续运行。请检查系统是否允许创建计划任务，" +
            "或在设置中把启动模式改为“管理员启动”或“普通启动”。");
        return false;
    }

    private bool RelaunchWithUac()
    {
        try
        {
            var arguments = new[]
            {
                AppIdentity.ElevatedRelaunchArgument,
                startHiddenToTray ? AppIdentity.StartupLaunchArgument : null,
                isUpdateRestart ? AppIdentity.UpdateRestartArgument : null
            };

            using var elevated = Process.Start(new ProcessStartInfo
            {
                FileName = AppIdentity.GetLaunchExecutablePath(),
                Arguments = string.Join(' ', arguments.Where(argument => argument is not null)),
                UseShellExecute = true,
                Verb = "runas"
            });
            AppLogger.Information("ElevationLauncher", "elevation-relaunch", "Restarting the app with administrator rights.");
            requestClose();
            return true;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == Win32ErrorCancelled)
        {
            AppLogger.Information("ElevationLauncher", "elevation-cancelled", "The user cancelled the UAC prompt; the app will not start.");
            AbortStartup(
                "你取消了管理员权限请求，WuGesture 不会启动。\n\n如需以普通权限使用，请把启动模式改为“普通启动”。");
            return true;
        }
        catch (Exception exception)
        {
            AppLogger.Error("ElevationLauncher", "elevation-failed", "Unable to relaunch with administrator rights; the app will not start.", exception);
            AbortStartup(
                "无法以管理员身份启动 WuGesture，应用不会启动。\n\n请检查系统 UAC 设置后重试，或把启动模式改为“普通启动”。");
            return true;
        }
    }

    private static void RemoveScheduledTaskIfPresent()
    {
        try
        {
            if (ScheduledTaskLauncher.Query() == ScheduledTaskState.Missing)
            {
                return;
            }

            if (!ScheduledTaskLauncher.TryUnregister(out var failure))
            {
                AppLogger.Warning("ElevationLauncher", "scheduled-task-cleanup-failed", failure);
            }
        }
        catch (Exception exception)
        {
            AppLogger.Warning(
                "ElevationLauncher",
                "scheduled-task-cleanup-failed",
                "Unable to remove the leftover scheduled task.",
                exception);
        }
    }

    private void AbortStartup(string message)
    {
        ShowWarning(message);
        requestClose();
    }

    private void ShowWarning(string message)
    {
        if (suppressDialogs)
        {
            return;
        }

        MessageBox.Show(
            message,
            $"{AppIdentity.DisplayName} - 需要管理员权限",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
