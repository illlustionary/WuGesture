using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureConfigNormalizerTests
{
    [Fact]
    public void Normalize_FillsMissingSections()
    {
        var config = GestureConfigNormalizer.Normalize(null);

        Assert.NotNull(config.Rules);
        Assert.NotNull(config.Applications);
        Assert.NotNull(config.Categories);
        Assert.NotNull(config.EdgeActions);
        Assert.NotNull(config.UiSettings);
        Assert.NotNull(config.UiSettings.LevelOsd);
        Assert.NotNull(config.UiSettings.AppBehavior);
    }

    [Fact]
    public void Normalize_TrimsRuleArgumentsAndDropsEmptyOnes()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Action = new GestureActionConfig
                    {
                        Type = GestureConfigContract.ActionTypes.Program,
                        Path = @"  C:\Tools\a.exe ",
                        Arguments = [" --flag ", "", "   "]
                    }
                }
            ]
        };

        GestureConfigNormalizer.Normalize(config);

        var action = config.Rules[0].Action;
        Assert.Equal(@"C:\Tools\a.exe", action.Path);
        var argument = Assert.Single(action.Arguments);
        Assert.Equal("--flag", argument);
    }

    [Fact]
    public void Normalize_TrimsAndDeduplicatesCategories()
    {
        var config = new GestureConfig { Categories = [" 工具 ", "", "工具", "浏览器"] };

        GestureConfigNormalizer.Normalize(config);

        Assert.Equal(2, config.Categories.Count);
        Assert.Equal("工具", config.Categories[0]);
        Assert.Equal("浏览器", config.Categories[1]);
    }

    [Fact]
    public void Normalize_DeduplicatesApplicationCategoriesIgnoringCase()
    {
        var config = new GestureConfig
        {
            Applications =
            [
                new GestureApplicationConfig { Name = "a.exe", Categories = ["Tools", " tools ", ""] }
            ]
        };

        GestureConfigNormalizer.Normalize(config);

        var category = Assert.Single(config.Applications[0].Categories);
        Assert.Equal("Tools", category);
    }

    [Fact]
    public void Normalize_ClampsOutOfRangeUiValuesToTheirBounds()
    {
        var config = new GestureConfig();
        config.UiSettings.GestureHint.DisplayDurationMs = -1;
        config.UiSettings.GestureHint.WidthPercent = 5;
        config.UiSettings.GestureSensitivity.Percent = 500;
        config.UiSettings.LevelOsd.CornerRadius = 500;

        GestureConfigNormalizer.Normalize(config);

        Assert.Equal(0, config.UiSettings.GestureHint.DisplayDurationMs);
        Assert.Equal(10, config.UiSettings.GestureHint.WidthPercent);
        Assert.Equal(200, config.UiSettings.GestureSensitivity.Percent);
        Assert.Equal(95, config.UiSettings.LevelOsd.CornerRadius);
    }

    [Fact]
    public void Normalize_KeepsCornerRadiusWithinHalfOfTheOsdSize()
    {
        var config = new GestureConfig();
        config.UiSettings.LevelOsd.Width = 120;
        config.UiSettings.LevelOsd.Height = 100;
        config.UiSettings.LevelOsd.CornerRadius = 50;

        GestureConfigNormalizer.Normalize(config);

        Assert.Equal(50, config.UiSettings.LevelOsd.CornerRadius);
    }

    [Fact]
    public void Normalize_StripsTheOpaqueAlphaPrefixFromTrailColors()
    {
        var config = new GestureConfig();
        config.UiSettings.MouseTrail.InactiveColor = " #ff112233 ";
        config.UiSettings.MouseTrail.ActiveColor = "  ";

        GestureConfigNormalizer.Normalize(config);

        Assert.Equal("#112233", config.UiSettings.MouseTrail.InactiveColor);
        Assert.Equal(new MouseTrailUiSettings().ActiveColor, config.UiSettings.MouseTrail.ActiveColor);
    }

    [Fact]
    public void Normalize_FallsBackForUnknownContractValues()
    {
        var config = new GestureConfig();
        config.UiSettings.AppBehavior.CloseButtonBehavior = "unknown";
        config.UiSettings.AppBehavior.TargetWindowMode = "somewhere";
        config.UiSettings.Appearance.Theme = "neon";
        config.UiSettings.Update.Source = "GitHub";

        GestureConfigNormalizer.Normalize(config);

        Assert.Equal(
            GestureConfigContract.CloseButtonBehaviors.MinimizeToTray,
            config.UiSettings.AppBehavior.CloseButtonBehavior);
        Assert.Equal(
            GestureConfigContract.WindowTargetModes.StartWindow,
            config.UiSettings.AppBehavior.TargetWindowMode);
        Assert.Equal(GestureConfigContract.AppearanceThemes.System, config.UiSettings.Appearance.Theme);
        Assert.Equal(GestureConfigContract.UpdateSources.GitHub, config.UiSettings.Update.Source);
    }

    [Fact]
    public void Normalize_CleansExcludedApplications()
    {
        var config = new GestureConfig();
        config.UiSettings.AppBehavior.ExcludedApplications =
        [
            new ExcludedApplicationConfig { Name = " notepad.exe ", Path = " ", DisplayName = " " },
            new ExcludedApplicationConfig { Name = "notepad", DisplayName = "记事本" },
            new ExcludedApplicationConfig { Name = @" C:\Tools\code.exe ", DisplayName = "Visual Studio Code" },
            new ExcludedApplicationConfig { Name = " ", Path = " " }
        ];

        GestureConfigNormalizer.Normalize(config);

        Assert.Equal(2, config.UiSettings.AppBehavior.ExcludedApplications.Count);
        var notepad = config.UiSettings.AppBehavior.ExcludedApplications[0];
        Assert.Equal("notepad", notepad.Name);
        Assert.Equal("notepad", notepad.DisplayName);
        var code = config.UiSettings.AppBehavior.ExcludedApplications[1];
        Assert.Equal("code", code.Name);
        Assert.Equal("Visual Studio Code", code.DisplayName);
    }

    [Fact]
    public void Normalize_TrimsWebDavSettingsButKeepsThePassword()
    {
        var config = new GestureConfig();
        config.UiSettings.WebDav.Address = "  https://dav.example.com ";
        config.UiSettings.WebDav.UserName = " me ";
        config.UiSettings.WebDav.Password = " secret ";
        config.UiSettings.WebDav.RemotePath = " /backup ";

        GestureConfigNormalizer.Normalize(config);

        Assert.Equal("https://dav.example.com", config.UiSettings.WebDav.Address);
        Assert.Equal("me", config.UiSettings.WebDav.UserName);
        Assert.Equal(" secret ", config.UiSettings.WebDav.Password);
        Assert.Equal("/backup", config.UiSettings.WebDav.RemotePath);
    }

    [Fact]
    public void Normalize_ClampsMouseTrailAndGestureHintNumbersToEditorLimits()
    {
        var config = new GestureConfig();
        config.UiSettings.MouseTrail.InactiveThickness = 120f;
        config.UiSettings.MouseTrail.ActiveThickness = -4f;
        config.UiSettings.MouseTrail.InactiveOpacity = 220;
        config.UiSettings.MouseTrail.ActiveOpacity = -10;
        config.UiSettings.GestureHint.FontFamily = "  ";
        config.UiSettings.GestureHint.FontSize = 400f;
        config.UiSettings.GestureHint.CornerRadius = -30f;
        config.UiSettings.GestureHint.BackgroundOpacity = 400;

        GestureConfigNormalizer.Normalize(config);

        var mouseTrail = config.UiSettings.MouseTrail;
        Assert.Equal(20f, mouseTrail.InactiveThickness);
        Assert.Equal(1f, mouseTrail.ActiveThickness);
        Assert.Equal(100, mouseTrail.InactiveOpacity);
        Assert.Equal(0, mouseTrail.ActiveOpacity);

        var gestureHint = config.UiSettings.GestureHint;
        Assert.Equal(new GestureHintUiSettings().FontFamily, gestureHint.FontFamily);
        Assert.Equal(48f, gestureHint.FontSize);
        Assert.Equal(0f, gestureHint.CornerRadius);
        Assert.Equal(100, gestureHint.BackgroundOpacity);
    }
}
