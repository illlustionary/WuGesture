using System.Collections.Concurrent;
using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

internal static class WebViewRulesPayloadFactory
{
    private const int PathProbeTtlMilliseconds = 3000;
    private const int MaxPathProbes = 256;
    private static readonly ConcurrentDictionary<string, PathProbe> PathProbes = new(StringComparer.OrdinalIgnoreCase);

    public static string Create(LoadedGestureConfig loadedConfig, string? systemAccent = null)
    {
        return JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.Rules,
            appVersion = AppIdentity.GetDisplayVersion(),
            configPath = loadedConfig.FilePath,
            uiSettings = CreateUiSettingsPayload(
                loadedConfig.Config.UiSettings,
                systemAccent ?? WindowsAccentColor.Read()),
            rules = loadedConfig.Config.Rules.Select(rule => new
            {
                scope = rule.Scope,
                mouseButton = string.IsNullOrWhiteSpace(rule.MouseButton) ? GestureConfigContract.MouseButtons.Right : rule.MouseButton,
                pattern = rule.Pattern,
                wheelModifier = rule.WheelModifier,
                executeOnWheel = rule.ExecuteOnWheel,
                actionName = rule.ActionName,
                actionType = string.IsNullOrWhiteSpace(rule.Action.Type) ? GestureConfigContract.ActionTypes.Hotkey : rule.Action.Type,
                keys = rule.Action.Keys,
                operation = rule.Action.Operation,
                amount = rule.Action.Amount,
                path = rule.Action.Path,
                arguments = rule.Action.Arguments,
                programName = Path.GetFileNameWithoutExtension(rule.Action.Path),
                programIcon = ProgramIcon(rule.Action.Path),
                programMissing = !string.IsNullOrWhiteSpace(rule.Action.Path) && !ProgramPathExists(rule.Action.Path)
            }).ToArray(),
            edgeActions = loadedConfig.Config.EdgeActions.Select(action => new
            {
                enabled = action.Enabled,
                triggerType = action.TriggerType,
                location = action.Location,
                wheelDirection = action.WheelDirection,
                frictionCount = action.FrictionCount,
                action = new
                {
                    type = string.IsNullOrWhiteSpace(action.Action.Type) ? GestureConfigContract.ActionTypes.Hotkey : action.Action.Type,
                    keys = action.Action.Keys,
                    operation = action.Action.Operation,
                    amount = action.Action.Amount
                }
            }).ToArray(),
            applications = loadedConfig.Config.Applications.Select(application => new
            {
                name = application.Name,
                displayName = application.DisplayName,
                path = application.Path,
                categories = application.Categories,
                icon = ProgramIcon(application.Path)
            }).ToArray(),
            categories = loadedConfig.Config.Categories
        });
    }

    /// <summary>
    /// <see cref="File.Exists"/> and icon extraction are synchronous probes; on a network share they can take
    /// long enough to stall the UI thread during save/reload. Probe results are cached for a few seconds, and a
    /// missing file skips icon extraction entirely.
    /// </summary>
    private static bool ProgramPathExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var now = Environment.TickCount64;
        if (PathProbes.TryGetValue(path, out var cached) &&
            now - cached.CheckedAtTick < PathProbeTtlMilliseconds)
        {
            return cached.Exists;
        }

        bool exists;
        try
        {
            exists = File.Exists(path);
        }
        catch
        {
            exists = false;
        }

        if (PathProbes.Count >= MaxPathProbes)
        {
            PathProbes.Clear();
        }

        PathProbes[path] = new PathProbe(exists, now);
        return exists;
    }

    private static string ProgramIcon(string path) =>
        ProgramPathExists(path) ? ApplicationIconDataUrl.FromPath(path) : "";

    private readonly record struct PathProbe(bool Exists, long CheckedAtTick);

    private static object CreateUiSettingsPayload(GestureUiSettings uiSettings, string systemAccent)
    {
        return new
        {
            appearance = new
            {
                theme = uiSettings.Appearance.Theme,
                colorSource = uiSettings.Appearance.ColorSource,
                seedColor = uiSettings.Appearance.SeedColor,
                schemeVariant = uiSettings.Appearance.SchemeVariant,
                systemAccent,
                lightTitleBarColor = uiSettings.Appearance.LightTitleBarColor,
                lightTitleBarTextColor = uiSettings.Appearance.LightTitleBarTextColor,
                darkTitleBarColor = uiSettings.Appearance.DarkTitleBarColor,
                darkTitleBarTextColor = uiSettings.Appearance.DarkTitleBarTextColor
            },
            sidebar = new
            {
                collapsed = uiSettings.Sidebar.Collapsed,
                revealDistance = uiSettings.Sidebar.RevealDistance
            },
            mouseTrail = uiSettings.MouseTrail,
            gestureHint = uiSettings.GestureHint,
            levelOsd = uiSettings.LevelOsd,
            gestureSensitivity = uiSettings.GestureSensitivity,
            appBehavior = new
            {
                launchAtStartup = uiSettings.AppBehavior.LaunchAtStartup,
                showConfigWindowOnLaunch = uiSettings.AppBehavior.ShowConfigWindowOnLaunch,
                startMode = uiSettings.AppBehavior.StartMode,
                closeButtonBehavior = uiSettings.AppBehavior.CloseButtonBehavior,
                targetWindowMode = uiSettings.AppBehavior.TargetWindowMode,
                disableGesturesInFullscreen = uiSettings.AppBehavior.DisableGesturesInFullscreen,
                disableGesturesOnDesktop = uiSettings.AppBehavior.DisableGesturesOnDesktop,
                disableEdgeActionsInFullscreen = uiSettings.AppBehavior.DisableEdgeActionsInFullscreen,
                excludedApplications = uiSettings.AppBehavior.ExcludedApplications.Select(application => new
                {
                    name = application.Name,
                    displayName = application.DisplayName,
                    path = application.Path,
                    disableEdgeActions = application.DisableEdgeActions,
                    icon = ProgramIcon(application.Path)
                }).ToArray()
            },
            webDav = uiSettings.WebDav,
            update = new
            {
                checkForUpdates = uiSettings.Update.CheckForUpdates,
                source = uiSettings.Update.Source
            },
            onboarding = new
            {
                welcomeCompleted = uiSettings.Onboarding.WelcomeCompleted
            }
        };
    }
}
