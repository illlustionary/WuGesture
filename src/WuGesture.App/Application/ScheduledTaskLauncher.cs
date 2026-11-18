using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace WuGesture.App;

internal enum ScheduledTaskState
{
    Unknown,
    Missing,
    Matches,
    PointsElsewhere
}

/// <summary>
/// Registers and launches the "run with highest privileges" scheduled task that lets WuGesture start
/// without a UAC prompt. Creating or deleting the task asks for administrator rights once; running it
/// never prompts, because Windows stores the consent on the task itself.
/// </summary>
internal static class ScheduledTaskLauncher
{
    public const string TaskName = "WuGestureElevated";

    private const string ElevatedHelperTimeoutHint = "请确认已在 UAC 提示中允许操作后重试。";
    private const int SchTasksTimeoutMs = 30_000;
    private const int RegistrationPollAttempts = 24;
    private const int RegistrationPollIntervalMs = 250;

    private static readonly XNamespace TaskNamespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";

    /// <summary>
    /// Makes sure the task exists and points at the executable that is running right now.
    /// </summary>
    public static bool EnsureRegistered(out string failure)
    {
        var executablePath = AppIdentity.GetLaunchExecutablePath();
        if (Query(executablePath) == ScheduledTaskState.Matches)
        {
            failure = "";
            return true;
        }

        return TryRegister(executablePath, out failure);
    }

    public static bool TryRegister(out string failure)
    {
        return TryRegister(AppIdentity.GetLaunchExecutablePath(), out failure);
    }

    public static bool TryRegister(string executablePath, out string failure)
    {
        failure = "";
        if (!ElevationState.IsRunningAsAdministrator())
        {
            if (!StartElevatedSelf(AppIdentity.CreateScheduledTaskArgument, out failure))
            {
                return false;
            }
        }
        else if (!Create(executablePath, out failure))
        {
            return false;
        }

        for (var attempt = 0; attempt < RegistrationPollAttempts; attempt++)
        {
            if (Query(executablePath) == ScheduledTaskState.Matches)
            {
                AppLogger.Information("ScheduledTaskLauncher", "task-registered", $"已注册计划任务 {TaskName}。");
                failure = "";
                return true;
            }

            Thread.Sleep(RegistrationPollIntervalMs);
        }

        failure = $"计划任务 {TaskName} 创建后仍未生效。{ElevatedHelperTimeoutHint}";
        AppLogger.Warning("ScheduledTaskLauncher", "task-register-timeout", failure);
        return false;
    }

    /// <summary>
    /// Removes the task. Returns true when the task is gone afterwards (including "it never existed").
    /// </summary>
    public static bool TryUnregister(out string failure)
    {
        failure = "";
        if (Query() == ScheduledTaskState.Missing)
        {
            return true;
        }

        if (!ElevationState.IsRunningAsAdministrator())
        {
            if (!StartElevatedSelf(AppIdentity.DeleteScheduledTaskArgument, out failure))
            {
                return false;
            }
        }
        else if (!Delete(out failure))
        {
            return false;
        }

        for (var attempt = 0; attempt < RegistrationPollAttempts; attempt++)
        {
            if (Query() == ScheduledTaskState.Missing)
            {
                AppLogger.Information("ScheduledTaskLauncher", "task-unregistered", $"已删除计划任务 {TaskName}。");
                failure = "";
                return true;
            }

            Thread.Sleep(RegistrationPollIntervalMs);
        }

        failure = $"计划任务 {TaskName} 删除后仍然存在。{ElevatedHelperTimeoutHint}";
        AppLogger.Warning("ScheduledTaskLauncher", "task-unregister-timeout", failure);
        return false;
    }

    /// <summary>
    /// Runs the registered task. Must be called from a process that is not elevated yet.
    /// </summary>
    public static bool Run(out string failure)
    {
        var result = RunSchTasks($"/Run /TN \"{TaskName}\"");
        if (result.ExitCode == 0)
        {
            AppLogger.Information("ScheduledTaskLauncher", "task-run", $"已通过计划任务 {TaskName} 启动。");
            failure = "";
            return true;
        }

        failure = $"无法启动计划任务 {TaskName}：{FirstLine(result.Error, result.Output)}";
        AppLogger.Warning("ScheduledTaskLauncher", "task-run-failed", failure);
        return false;
    }

    /// <summary>
    /// Creates the task. Requires an administrator token.
    /// </summary>
    public static bool Create(string executablePath, out string failure)
    {
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"WuGestureTask_{Guid.NewGuid():N}.xml");

        try
        {
            var xml = BuildTaskXml(executablePath);
            // Task Scheduler expects the XML file to be UTF-16 encoded.
            File.WriteAllText(tempPath, xml, Encoding.Unicode);
            var result = RunSchTasks($"/Create /TN \"{TaskName}\" /XML \"{tempPath}\" /F");
            if (result.ExitCode == 0)
            {
                failure = "";
                return true;
            }

            failure = $"创建计划任务 {TaskName} 失败：{FirstLine(result.Error, result.Output)}";
            AppLogger.Warning("ScheduledTaskLauncher", "task-create-failed", failure);
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            failure = $"创建计划任务 {TaskName} 失败：{exception.Message}";
            AppLogger.Warning("ScheduledTaskLauncher", "task-create-failed", failure, exception);
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Deletes the task. Requires an administrator token.
    /// </summary>
    public static bool Delete(out string failure)
    {
        var result = RunSchTasks($"/Delete /TN \"{TaskName}\" /F");
        if (result.ExitCode == 0)
        {
            failure = "";
            return true;
        }

        failure = $"删除计划任务 {TaskName} 失败：{FirstLine(result.Error, result.Output)}";
        AppLogger.Warning("ScheduledTaskLauncher", "task-delete-failed", failure);
        return false;
    }

    /// <summary>
    /// Starts a copy of this executable that waits for <paramref name="processId"/> to exit and then runs
    /// the task. Without the wait the task instance would race the single-instance mutex of the exiting
    /// process.
    /// </summary>
    public static bool StartHandover(int processId)
    {
        try
        {
            using var handover = Process.Start(new ProcessStartInfo
            {
                FileName = AppIdentity.GetLaunchExecutablePath(),
                Arguments = $"{AppIdentity.TaskHandoverArgument} {processId.ToString(CultureInfo.InvariantCulture)}",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (handover is null)
            {
                AppLogger.Warning("ScheduledTaskLauncher", "handover-failed", "无法启动计划任务交接进程。");
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            AppLogger.Error("ScheduledTaskLauncher", "handover-failed", "无法启动计划任务交接进程。", exception);
            return false;
        }
    }

    /// <summary>
    /// Body of the handover helper process: wait for the old instance, then run the task.
    /// </summary>
    public static int HandleHandover(int processId)
    {
        WaitForProcessExit(processId, TimeSpan.FromSeconds(15));
        return Run(out _) ? 0 : 1;
    }

    public static ScheduledTaskState Query()
    {
        return Query(TaskName, AppIdentity.GetLaunchExecutablePath());
    }

    public static ScheduledTaskState Query(string expectedExecutablePath)
    {
        return Query(TaskName, expectedExecutablePath);
    }

    internal static ScheduledTaskState Query(string taskName, string expectedExecutablePath)
    {
        var result = RunSchTasks($"/Query /TN \"{taskName}\" /XML");
        if (result.ExitCode != 0)
        {
            return string.IsNullOrWhiteSpace(result.Error) && string.IsNullOrWhiteSpace(result.Output)
                ? ScheduledTaskState.Unknown
                : ScheduledTaskState.Missing;
        }

        return ClassifyXml(result.Output, expectedExecutablePath);
    }

    internal static ScheduledTaskState ClassifyXml(string xml, string expectedExecutablePath)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return ScheduledTaskState.Missing;
        }

        try
        {
            var document = XDocument.Parse(xml);
            var command = document.Descendants(TaskNamespace + "Command").FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(command))
            {
                return ScheduledTaskState.PointsElsewhere;
            }

            return PathsEqual(command, expectedExecutablePath)
                ? ScheduledTaskState.Matches
                : ScheduledTaskState.PointsElsewhere;
        }
        catch (System.Xml.XmlException)
        {
            return ScheduledTaskState.Unknown;
        }
    }

    internal static string BuildTaskXml(string executablePath, string? userSid = null)
    {
        var sid = userSid ?? ElevationState.GetCurrentUserSid();
        if (string.IsNullOrWhiteSpace(sid))
        {
            throw new InvalidOperationException("无法获取当前用户 SID，不能创建计划任务。");
        }

        var workingDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new InvalidOperationException($"无法解析 {executablePath} 所在目录。");
        }

        var document = new XDocument(
            new XElement(TaskNamespace + "Task",
                new XAttribute("version", "1.2"),
                new XElement(TaskNamespace + "RegistrationInfo",
                    new XElement(TaskNamespace + "Date", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                    new XElement(TaskNamespace + "Author", AppIdentity.DisplayName),
                    new XElement(TaskNamespace + "Description", "以最高权限启动 WuGesture，避免每次启动弹出 UAC 提示。"),
                    new XElement(TaskNamespace + "URI", $"\\{TaskName}")),
                new XElement(TaskNamespace + "Triggers"),
                new XElement(TaskNamespace + "Principals",
                    new XElement(TaskNamespace + "Principal",
                        new XAttribute("id", "Author"),
                        new XElement(TaskNamespace + "UserId", sid),
                        new XElement(TaskNamespace + "LogonType", "InteractiveToken"),
                        new XElement(TaskNamespace + "RunLevel", "HighestAvailable"))),
                new XElement(TaskNamespace + "Settings",
                    new XElement(TaskNamespace + "MultipleInstancesPolicy", "Parallel"),
                    new XElement(TaskNamespace + "DisallowStartIfOnBatteries", "false"),
                    new XElement(TaskNamespace + "StopIfGoingOnBatteries", "false"),
                    new XElement(TaskNamespace + "AllowHardTerminate", "true"),
                    new XElement(TaskNamespace + "StartWhenAvailable", "false"),
                    new XElement(TaskNamespace + "RunOnlyIfNetworkAvailable", "false"),
                    new XElement(TaskNamespace + "IdleSettings",
                        new XElement(TaskNamespace + "StopOnIdleEnd", "true"),
                        new XElement(TaskNamespace + "RestartOnIdle", "false")),
                    new XElement(TaskNamespace + "AllowStartOnDemand", "true"),
                    new XElement(TaskNamespace + "Enabled", "true"),
                    new XElement(TaskNamespace + "Hidden", "false"),
                    new XElement(TaskNamespace + "RunOnlyIfIdle", "false"),
                    new XElement(TaskNamespace + "WakeToRun", "false"),
                    // Is a tray app that may stay open for weeks: no execution time limit.
                    new XElement(TaskNamespace + "ExecutionTimeLimit", "PT0S"),
                    new XElement(TaskNamespace + "Priority", "4")),
                new XElement(TaskNamespace + "Actions",
                    new XAttribute("Context", "Author"),
                    new XElement(TaskNamespace + "Exec",
                        new XElement(TaskNamespace + "Command", executablePath),
                        new XElement(TaskNamespace + "Arguments", AppIdentity.ScheduledLaunchArgument),
                        new XElement(TaskNamespace + "WorkingDirectory", workingDirectory)))));

        return $"<?xml version=\"1.0\" encoding=\"UTF-16\"?>{Environment.NewLine}{document}";
    }

    private static bool StartElevatedSelf(string argument, out string failure)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = AppIdentity.GetLaunchExecutablePath(),
                Arguments = argument,
                UseShellExecute = true,
                Verb = "runas"
            });

            if (process is null)
            {
                failure = "无法启动提权进程。";
                return false;
            }

            failure = "";
            TryWaitForExit(process, TimeSpan.FromSeconds(60));
            return true;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == Win32ErrorCancelled)
        {
            failure = "你取消了管理员权限请求，启动模式未改变。";
            AppLogger.Information("ScheduledTaskLauncher", "task-elevation-cancelled", failure);
            return false;
        }
        catch (Exception exception)
        {
            failure = $"无法请求管理员权限：{exception.Message}";
            AppLogger.Error("ScheduledTaskLauncher", "task-elevation-failed", failure, exception);
            return false;
        }
    }

    private static void TryWaitForExit(Process process, TimeSpan timeout)
    {
        try
        {
            process.WaitForExit((int)timeout.TotalMilliseconds);
        }
        catch (Exception exception)
        {
            AppLogger.Warning("ScheduledTaskLauncher", "task-elevation-wait-failed", "等待提权进程退出时失败。", exception);
        }
    }

    private static void WaitForProcessExit(int processId, TimeSpan timeout)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.WaitForExit((int)timeout.TotalMilliseconds);
        }
        catch (ArgumentException)
        {
            // The process is already gone, which is exactly what we are waiting for.
        }
        catch (Exception exception)
        {
            AppLogger.Warning("ScheduledTaskLauncher", "handover-wait-failed", $"等待进程 {processId} 退出时失败。", exception);
        }
    }

    private static SchTasksResult RunSchTasks(string arguments)
    {
        var commandInterpreter = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
        var startInfo = new ProcessStartInfo
        {
            FileName = commandInterpreter,
            Arguments = $"/c chcp 65001 > nul && schtasks {arguments}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(SchTasksTimeoutMs))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                return new SchTasksResult(-1, output, "schtasks 执行超时。");
            }

            return new SchTasksResult(process.ExitCode, output, error);
        }
        catch (Exception exception)
        {
            return new SchTasksResult(-1, "", exception.Message);
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string FirstLine(string primary, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(primary) ? fallback : primary;
        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "";
    }

    private const int Win32ErrorCancelled = 1223;

    private readonly record struct SchTasksResult(int ExitCode, string Output, string Error);
}
