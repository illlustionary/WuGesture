namespace WuGesture.App.GestureEngine;

internal static class GestureConfigNormalizer
{
    public static GestureConfig Normalize(GestureConfig? config)
    {
        config ??= new GestureConfig();
        config.Rules ??= [];
        config.Applications ??= [];
        config.EdgeActions ??= [];
        config.UiSettings ??= new GestureUiSettings();
        config.UiSettings.Appearance ??= new AppearanceUiSettings();
        config.UiSettings.MouseTrail ??= new MouseTrailUiSettings();
        config.UiSettings.GestureHint ??= new GestureHintUiSettings();
        config.UiSettings.LevelOsd ??= new LevelOsdUiSettings();
        config.UiSettings.GestureSensitivity ??= new GestureSensitivityUiSettings();
        config.UiSettings.AppBehavior ??= new AppBehaviorUiSettings();
        config.UiSettings.WebDav ??= new WebDavUiSettings();
        NormalizeApplications(config.Applications);
        NormalizeUiSettings(config.UiSettings);
        return config;
    }

    private static void NormalizeApplications(IEnumerable<GestureApplicationConfig> applications)
    {
        foreach (var application in applications)
        {
            application.Categories ??= [];
            if (application.Categories.Count == 0 && !string.IsNullOrWhiteSpace(application.Category))
            {
                application.Categories.Add(application.Category);
            }

            application.Categories = application.Categories
                .Select(category => category.Trim())
                .Where(category => category.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            application.Category = null;
        }
    }

    private static void NormalizeUiSettings(GestureUiSettings settings)
    {
        var appearance = settings.Appearance;
        appearance.Theme = NormalizeAppearanceTheme(appearance.Theme);

        var mouseTrail = settings.MouseTrail;
        mouseTrail.Enabled ??= true;
        var legacyThickness = mouseTrail.Thickness > 0 ? mouseTrail.Thickness : 3f;
        if (mouseTrail.InactiveThickness <= 0 ||
            (Math.Abs(mouseTrail.InactiveThickness - 3f) < 0.001f && Math.Abs(legacyThickness - 3f) > 0.001f))
        {
            mouseTrail.InactiveThickness = legacyThickness;
        }

        if (mouseTrail.ActiveThickness <= 0 ||
            (Math.Abs(mouseTrail.ActiveThickness - 3f) < 0.001f && Math.Abs(legacyThickness - 3f) > 0.001f))
        {
            mouseTrail.ActiveThickness = legacyThickness;
        }

        mouseTrail.Thickness = Math.Max(1f, mouseTrail.InactiveThickness);

        var gestureHint = settings.GestureHint;
        gestureHint.Enabled ??= true;
        gestureHint.DisplayDurationMs = ClampInteger(gestureHint.DisplayDurationMs, 0, 10000, 1800);
        gestureHint.FadeDurationMs = ClampInteger(gestureHint.FadeDurationMs, 0, 1000, 240);
        gestureHint.WidthPercent = ClampInteger(gestureHint.WidthPercent, 10, 90, 28);
        gestureHint.HeightPercent = ClampInteger(gestureHint.HeightPercent, 5, 40, 11);
        gestureHint.BottomOffsetPercent = ClampInteger(gestureHint.BottomOffsetPercent, 0, 100, 13);
        if (gestureHint.BottomOffset < 0)
        {
            gestureHint.BottomOffset = 140;
        }

        var levelOsd = settings.LevelOsd;
        levelOsd.Enabled ??= true;
        levelOsd.DisplayDurationMs = ClampInteger(levelOsd.DisplayDurationMs, 0, 10000, 1800);
        levelOsd.FadeDurationMs = ClampInteger(levelOsd.FadeDurationMs, 0, 1000, 240);
        levelOsd.BackgroundColor = NormalizeColor(levelOsd.BackgroundColor, "#28282C");
        levelOsd.BackgroundOpacity = ClampInteger(levelOsd.BackgroundOpacity, 0, 100, 88);
        levelOsd.TextColor = NormalizeColor(levelOsd.TextColor, "#DCDCDC");
        levelOsd.TrackColor = NormalizeColor(levelOsd.TrackColor, "#464646");
        levelOsd.VolumeColor = NormalizeColor(levelOsd.VolumeColor, "#64C8FF");
        levelOsd.BrightnessColor = NormalizeColor(levelOsd.BrightnessColor, "#FFC828");
        levelOsd.Width = ClampInteger(levelOsd.Width, 120, 480, 210);
        levelOsd.Height = ClampInteger(levelOsd.Height, 100, 420, 190);
        levelOsd.CornerRadius = ClampInteger(levelOsd.CornerRadius, 0, Math.Min(levelOsd.Width, levelOsd.Height) / 2, 22);
        levelOsd.Position = NormalizeLevelOsdPosition(levelOsd.Position);
        levelOsd.OffsetX = ClampInteger(levelOsd.OffsetX, -2000, 2000, 0);
        levelOsd.OffsetY = ClampInteger(levelOsd.OffsetY, -2000, 2000, 0);

        var gestureSensitivity = settings.GestureSensitivity;
        gestureSensitivity.Percent = ClampInteger(gestureSensitivity.Percent, 0, 200, 110);

        var appBehavior = settings.AppBehavior;
        appBehavior.CloseButtonBehavior = NormalizeCloseButtonBehavior(appBehavior.CloseButtonBehavior);
        appBehavior.TargetWindowMode = NormalizeWindowTargetMode(appBehavior.TargetWindowMode);
        appBehavior.ExcludedApplications = NormalizeExcludedApplications(appBehavior.ExcludedApplications);

        var webDav = settings.WebDav;
        webDav.Address = (webDav.Address ?? "").Trim();
        webDav.UserName = (webDav.UserName ?? "").Trim();
        webDav.Password ??= "";
        webDav.RemotePath = (webDav.RemotePath ?? "").Trim();
    }

    private static string NormalizeCloseButtonBehavior(string? value)
    {
        return value is
            GestureConfigContract.CloseButtonBehaviors.MinimizeToTray or
            GestureConfigContract.CloseButtonBehaviors.MinimizeToTaskbar or
            GestureConfigContract.CloseButtonBehaviors.Exit
            ? value
            : GestureConfigContract.CloseButtonBehaviors.MinimizeToTray;
    }

    private static string NormalizeWindowTargetMode(string? value)
    {
        return value is GestureConfigContract.WindowTargetModes.CurrentWindow
            ? value
            : GestureConfigContract.WindowTargetModes.StartWindow;
    }

    private static string NormalizeAppearanceTheme(string? value)
    {
        return value is
            GestureConfigContract.AppearanceThemes.System or
            GestureConfigContract.AppearanceThemes.Light or
            GestureConfigContract.AppearanceThemes.Dark
            ? value
            : GestureConfigContract.AppearanceThemes.System;
    }

    private static string NormalizeLevelOsdPosition(string? value)
    {
        return value is
            GestureConfigContract.LevelOsdPositions.Center or
            GestureConfigContract.LevelOsdPositions.TopCenter or
            GestureConfigContract.LevelOsdPositions.BottomCenter or
            GestureConfigContract.LevelOsdPositions.TopLeft or
            GestureConfigContract.LevelOsdPositions.TopRight or
            GestureConfigContract.LevelOsdPositions.BottomLeft or
            GestureConfigContract.LevelOsdPositions.BottomRight
            ? value
            : GestureConfigContract.LevelOsdPositions.Center;
    }

    private static string NormalizeColor(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static List<ExcludedApplicationConfig> NormalizeExcludedApplications(IEnumerable<ExcludedApplicationConfig>? applications)
    {
        var result = new List<ExcludedApplicationConfig>();
        foreach (var application in applications ?? [])
        {
            var normalized = NormalizeExcludedApplication(application);
            if (string.IsNullOrWhiteSpace(normalized.Name) && string.IsNullOrWhiteSpace(normalized.Path))
            {
                continue;
            }

            if (!result.Any(existing => IsSameApplicationIdentity(existing, normalized)))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static ExcludedApplicationConfig NormalizeExcludedApplication(ExcludedApplicationConfig application)
    {
        var name = ApplicationIdentityNormalizer.NormalizeProcessName(application.Name);
        var path = (application.Path ?? "").Trim();
        var displayName = (application.DisplayName ?? "").Trim();
        return new ExcludedApplicationConfig
        {
            Name = name,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName,
            Path = path,
            DisableEdgeActions = application.DisableEdgeActions
        };
    }

    private static bool IsSameApplicationIdentity(ExcludedApplicationConfig left, ExcludedApplicationConfig right)
    {
        if (!string.IsNullOrWhiteSpace(left.Path) && !string.IsNullOrWhiteSpace(right.Path))
        {
            return string.Equals(left.Path.Trim(), right.Path.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            ApplicationIdentityNormalizer.NormalizeProcessName(left.Name),
            ApplicationIdentityNormalizer.NormalizeProcessName(right.Name),
            StringComparison.OrdinalIgnoreCase);
    }

    private static int ClampInteger(int value, int min, int max, int fallback)
    {
        return value < min || value > max ? fallback : value;
    }
}
