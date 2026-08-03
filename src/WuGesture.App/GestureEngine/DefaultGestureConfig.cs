namespace WuGesture.App.GestureEngine;

public static class DefaultGestureConfig
{
    public static GestureConfig Create()
    {
        var config = GestureConfigMapper.FromRules(DefaultGestureRules.Create());
        config.SchemaVersion = GestureConfigContract.Schema.CurrentVersion;
        config.Applications = [];
        config.UiSettings = new GestureUiSettings
        {
            Appearance = new AppearanceUiSettings
            {
                Theme = GestureConfigContract.AppearanceThemes.Dark
            },
            Sidebar = new SidebarUiSettings
            {
                Collapsed = true
            },
            MouseTrail = new MouseTrailUiSettings
            {
                Enabled = true,
                InactiveColor = "#BDBDBD",
                ActiveColor = "#87CEEB",
                InactiveThickness = 3,
                ActiveThickness = 3,
                InactiveOpacity = 74,
                ActiveOpacity = 100
            },
            GestureHint = new GestureHintUiSettings
            {
                Enabled = true,
                DisplayDurationMs = 300,
                WidthPercent = 10,
                AutoWidth = true,
                HeightPercent = 5,
                BottomOffsetPercent = 6
            },
            LevelOsd = new LevelOsdUiSettings
            {
                Enabled = true,
                DisplayDurationMs = 1800,
                FadeDurationMs = 240,
                BackgroundColor = "#28282C",
                BackgroundOpacity = 88,
                TextColor = "#DCDCDC",
                TrackColor = "#464646",
                VolumeColor = "#64C8FF",
                BrightnessColor = "#FFC828",
                Width = 210,
                Height = 190,
                CornerRadius = 22,
                Position = GestureConfigContract.LevelOsdPositions.Center,
                OffsetX = 0,
                OffsetY = 0
            },
            GestureSensitivity = new GestureSensitivityUiSettings
            {
                Percent = 110
            },
            AppBehavior = new AppBehaviorUiSettings
            {
                LaunchAtStartup = true,
                ShowConfigWindowOnLaunch = true,
                RunAsAdministrator = false,
                CloseButtonBehavior = GestureConfigContract.CloseButtonBehaviors.MinimizeToTray,
                TargetWindowMode = GestureConfigContract.WindowTargetModes.StartWindow,
                GesturePaused = false,
                DisableGesturesInFullscreen = false,
                DisableEdgeActionsInFullscreen = false,
                ExcludedApplications = []
            },
            WebDav = new WebDavUiSettings()
        };
        config.EdgeActions =
        [
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.TopLeft, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.TopRight, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win", "Tab"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.BottomLeft, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win"]
            }, enabled: true),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Corner, GestureConfigContract.EdgeLocations.BottomRight, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Win", "D"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Left, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Control", "Shift", "Escape"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Right, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["Control", "Shift", "Escape"]
            }, enabled: true),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Top, "", new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Hotkey,
                Keys = ["M", "I", "K", "U"]
            }),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Friction, GestureConfigContract.EdgeLocations.Bottom, "", new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Left, GestureConfigContract.WheelDirections.Up, new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Left, GestureConfigContract.WheelDirections.Down, new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Right, GestureConfigContract.WheelDirections.Up, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Volume,
                Operation = GestureConfigContract.Operations.Increase,
                Amount = 2
            }, enabled: true),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Right, GestureConfigContract.WheelDirections.Down, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Volume,
                Operation = GestureConfigContract.Operations.Decrease,
                Amount = 2
            }, enabled: true),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Top, GestureConfigContract.WheelDirections.Up, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Brightness,
                Operation = GestureConfigContract.Operations.Increase
            }, enabled: true),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Top, GestureConfigContract.WheelDirections.Down, new GestureActionConfig
            {
                Type = GestureConfigContract.ActionTypes.Brightness,
                Operation = GestureConfigContract.Operations.Decrease
            }, enabled: true),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Bottom, GestureConfigContract.WheelDirections.Up, new GestureActionConfig()),
            CreateEdgeAction(GestureConfigContract.EdgeTriggerTypes.Wheel, GestureConfigContract.EdgeLocations.Bottom, GestureConfigContract.WheelDirections.Down, new GestureActionConfig())
        ];

        return config;
    }

    private static EdgeActionConfig CreateEdgeAction(
        string triggerType,
        string location,
        string wheelDirection,
        GestureActionConfig action,
        bool enabled = false)
    {
        return new EdgeActionConfig
        {
            Enabled = enabled,
            TriggerType = triggerType,
            Location = location,
            WheelDirection = wheelDirection,
            FrictionCount = 4,
            Action = action
        };
    }
}
