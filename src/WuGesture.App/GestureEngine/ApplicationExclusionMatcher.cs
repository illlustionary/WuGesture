using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

public sealed class ApplicationExclusionMatcher
{
    private IReadOnlyList<ExcludedApplicationConfig> applications;

    public ApplicationExclusionMatcher(IEnumerable<ExcludedApplicationConfig>? applications = null)
    {
        this.applications = NormalizeApplications(applications);
    }

    public void Update(IEnumerable<ExcludedApplicationConfig>? nextApplications)
    {
        applications = NormalizeApplications(nextApplications);
    }

    public bool IsGestureExcluded()
    {
        return IsGestureExcluded(GetForegroundWindow());
    }

    public bool IsGestureExcluded(IntPtr window)
    {
        return TryGetApplication(window, out var appName, out var appPath) &&
            applications.Any(application => IsMatch(application, appName, appPath));
    }

    public bool IsEdgeActionExcluded()
    {
        return TryGetApplication(GetForegroundWindow(), out var appName, out var appPath) &&
            applications.Any(application => application.DisableEdgeActions && IsMatch(application, appName, appPath));
    }

    private static IReadOnlyList<ExcludedApplicationConfig> NormalizeApplications(IEnumerable<ExcludedApplicationConfig>? source)
    {
        return (source ?? [])
            .Select(application => new ExcludedApplicationConfig
            {
                Name = ApplicationIdentityNormalizer.NormalizeProcessName(application.Name),
                DisplayName = (application.DisplayName ?? "").Trim(),
                Path = (application.Path ?? "").Trim(),
                DisableEdgeActions = application.DisableEdgeActions
            })
            .Where(application => application.Name.Length > 0 || application.Path.Length > 0)
            .ToArray();
    }

    private static bool IsMatch(ExcludedApplicationConfig application, string appName, string appPath)
    {
        if (!string.IsNullOrWhiteSpace(application.Path) &&
            !string.IsNullOrWhiteSpace(appPath) &&
            string.Equals(application.Path.Trim(), appPath.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(application.Name) &&
            string.Equals(ApplicationIdentityNormalizer.NormalizeProcessName(application.Name), appName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetApplication(IntPtr window, out string appName, out string appPath)
    {
        appName = "";
        appPath = "";

        if (window == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(window, out var processId);
        if (processId <= 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            appName = ApplicationIdentityNormalizer.NormalizeProcessName(process.ProcessName);
            try
            {
                appPath = process.MainModule?.FileName ?? "";
            }
            catch
            {
                appPath = "";
            }

            return appName.Length > 0 || appPath.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);
}
