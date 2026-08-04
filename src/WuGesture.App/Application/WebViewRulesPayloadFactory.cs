using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

internal static class WebViewRulesPayloadFactory
{
    public static string Create(LoadedGestureConfig loadedConfig)
    {
        return JsonSerializer.Serialize(new
        {
            type = WebViewMessageTypes.Rules,
            appVersion = AppIdentity.GetDisplayVersion(),
            configPath = loadedConfig.FilePath,
            uiSettings = CreateUiSettingsPayload(loadedConfig.Config.UiSettings),
            rules = loadedConfig.Config.Rules.Select(rule => new
            {
                scope = rule.Scope,
                mouseButton = string.IsNullOrWhiteSpace(rule.MouseButton) ? GestureConfigContract.MouseButtons.Right : rule.MouseButton,
                pattern = rule.Pattern,
                actionName = rule.ActionName,
                actionType = string.IsNullOrWhiteSpace(rule.Action.Type) ? GestureConfigContract.ActionTypes.Hotkey : rule.Action.Type,
                keys = rule.Action.Keys,
                operation = rule.Action.Operation,
                amount = rule.Action.Amount
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
                icon = ApplicationIconDataUrl.FromExecutable(application.Path)
            }).ToArray(),
            categories = loadedConfig.Config.Categories
        });
    }

    private static object CreateUiSettingsPayload(GestureUiSettings uiSettings)
    {
        return new
        {
            appearance = new
            {
                theme = uiSettings.Appearance.Theme,
                lightTitleBarColor = uiSettings.Appearance.LightTitleBarColor,
                lightTitleBarTextColor = uiSettings.Appearance.LightTitleBarTextColor,
                darkTitleBarColor = uiSettings.Appearance.DarkTitleBarColor,
                darkTitleBarTextColor = uiSettings.Appearance.DarkTitleBarTextColor
            },
            sidebar = new
            {
                collapsed = uiSettings.Sidebar.Collapsed
            },
            mouseTrail = uiSettings.MouseTrail,
            gestureHint = uiSettings.GestureHint,
            levelOsd = uiSettings.LevelOsd,
            gestureSensitivity = uiSettings.GestureSensitivity,
            appBehavior = new
            {
                launchAtStartup = uiSettings.AppBehavior.LaunchAtStartup,
                showConfigWindowOnLaunch = uiSettings.AppBehavior.ShowConfigWindowOnLaunch,
                runAsAdministrator = uiSettings.AppBehavior.RunAsAdministrator,
                closeButtonBehavior = uiSettings.AppBehavior.CloseButtonBehavior,
                targetWindowMode = uiSettings.AppBehavior.TargetWindowMode,
                gesturePaused = uiSettings.AppBehavior.GesturePaused,
                disableGesturesInFullscreen = uiSettings.AppBehavior.DisableGesturesInFullscreen,
                disableEdgeActionsInFullscreen = uiSettings.AppBehavior.DisableEdgeActionsInFullscreen,
                excludedApplications = uiSettings.AppBehavior.ExcludedApplications.Select(application => new
                {
                    name = application.Name,
                    displayName = application.DisplayName,
                    path = application.Path,
                    disableEdgeActions = application.DisableEdgeActions,
                    icon = ApplicationIconDataUrl.FromExecutable(application.Path)
                }).ToArray()
            },
            webDav = uiSettings.WebDav
        };
    }
}
