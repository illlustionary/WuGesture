using System.Runtime.InteropServices;

namespace WuGesture.App.GestureEngine;

public sealed class ApplicationExclusionMatcher
{
    private IReadOnlyList<ExcludedApplicationConfig> applications;
    private bool hasPathRules;

    public ApplicationExclusionMatcher(IEnumerable<ExcludedApplicationConfig>? applications = null)
    {
        this.applications = NormalizeApplications(applications);
        hasPathRules = this.applications.Any(application => application.Path.Length > 0);
    }

    public void Update(IEnumerable<ExcludedApplicationConfig>? nextApplications)
    {
        applications = NormalizeApplications(nextApplications);
        hasPathRules = applications.Any(application => application.Path.Length > 0);
    }

    public bool IsGestureExcluded()
    {
        if (applications.Count == 0)
        {
            return false;
        }

        return IsGestureExcluded(GetForegroundWindow());
    }

    public bool IsGestureExcluded(IntPtr window)
    {
        if (applications.Count == 0)
        {
            return false;
        }

        return TryGetApplication(window, hasPathRules, out var appName, out var appPath) &&
            applications.Any(application => IsMatch(application, appName, appPath));
    }

    public bool IsEdgeActionExcluded()
    {
        if (applications.Count == 0)
        {
            return false;
        }

        return TryGetApplication(GetForegroundWindow(), hasPathRules, out var appName, out var appPath) &&
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

    private static bool TryGetApplication(IntPtr window, bool includePath, out string appName, out string appPath)
    {
        appName = "";
        appPath = "";

        if (!WindowProcessCache.TryGet(window, includePath, out var process))
        {
            return false;
        }

        appName = ApplicationIdentityNormalizer.NormalizeProcessName(process.ProcessName);
        if (includePath)
        {
            appPath = process.ExecutablePath.Trim();
        }

        return appName.Length > 0 || appPath.Length > 0;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
