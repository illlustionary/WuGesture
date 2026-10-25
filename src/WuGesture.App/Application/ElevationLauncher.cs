using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

/// <summary>
/// Restarts the app through the UAC prompt when the configuration asks for administrator rights.
/// The owning form only decides what "close" means for it.
/// </summary>
internal sealed class ElevationLauncher
{
    private const int Win32ErrorCancelled = 1223;
    private readonly bool startHiddenToTray;
    private readonly bool suppressDialogs;
    private readonly bool isUpdateRestart;
    private readonly Action requestClose;

    public ElevationLauncher(bool startHiddenToTray, bool suppressDialogs, bool isUpdateRestart, Action requestClose)
    {
        this.startHiddenToTray = startHiddenToTray;
        this.suppressDialogs = suppressDialogs;
        this.isUpdateRestart = isUpdateRestart;
        this.requestClose = requestClose;
    }

    /// <summary>
    /// Returns true when the current process must stop because an elevated instance took over, or because
    /// elevation was refused and running without it would leave the gesture service with partial rights.
    /// </summary>
    public bool TryRelaunch(AppBehaviorUiSettings settings)
    {
        if (!settings.RunAsAdministrator || IsRunningAsAdministrator())
        {
            return false;
        }

        try
        {
            var arguments = new[]
            {
                AppIdentity.ElevatedRelaunchArgument,
                startHiddenToTray ? AppIdentity.StartupLaunchArgument : null,
                isUpdateRestart ? AppIdentity.UpdateRestartArgument : null
            };

            Process.Start(new ProcessStartInfo
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
                "你取消了管理员权限请求，WuGesture 不会启动。\n\n如需以普通权限使用，请先在设置中关闭“以管理员身份打开”。");
            return true;
        }
        catch (Exception exception)
        {
            AppLogger.Error("ElevationLauncher", "elevation-failed", "Unable to relaunch with administrator rights; the app will not start.", exception);
            AbortStartup(
                "无法以管理员身份启动 WuGesture，应用不会启动。\n\n请检查系统 UAC 设置后重试，或在设置中关闭“以管理员身份打开”。");
            return true;
        }
    }

    private void AbortStartup(string message)
    {
        if (!suppressDialogs)
        {
            MessageBox.Show(
                message,
                $"{AppIdentity.DisplayName} - 需要管理员权限",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        requestClose();
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
