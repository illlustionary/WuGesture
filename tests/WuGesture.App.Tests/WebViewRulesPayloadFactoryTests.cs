using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class WebViewRulesPayloadFactoryTests
{
    [Theory]
    [InlineData("#FFBDBDBD", "#BDBDBD")]
    [InlineData("#FF87CEEB", "#87CEEB")]
    public void Normalize_ConvertsLegacyOpaqueMouseTrailColorsToCssRgb(string sourceColor, string expectedColor)
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings
            {
                MouseTrail = new MouseTrailUiSettings
                {
                    InactiveColor = sourceColor,
                    ActiveColor = sourceColor
                }
            }
        });

        Assert.Equal(expectedColor, config.UiSettings.MouseTrail.InactiveColor);
        Assert.Equal(expectedColor, config.UiSettings.MouseTrail.ActiveColor);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("SYSTEM")]
    public void Normalize_NormalizesInvalidAppearanceThemeToSystem(string theme)
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings
            {
                Appearance = new AppearanceUiSettings { Theme = theme }
            }
        });

        Assert.Equal(GestureConfigContract.AppearanceThemes.System, config.UiSettings.Appearance.Theme);
    }

    [Fact]
    public void Normalize_UsesSystemAppearanceThemeWhenMissing()
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings { Appearance = null! }
        });

        Assert.Equal(GestureConfigContract.AppearanceThemes.System, config.UiSettings.Appearance.Theme);
    }

    [Fact]
    public void Create_DoesNotIncludeUnusedHostAppInfo()
    {
        var config = DefaultGestureConfig.Create();
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        Assert.Equal(WebViewMessageTypes.Rules, document.RootElement.GetProperty("type").GetString());
        Assert.False(document.RootElement.TryGetProperty("appInfo", out _));
    }

    [Fact]
    public void Create_IncludesApplicationVersion()
    {
        var config = DefaultGestureConfig.Create();
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        Assert.Equal(
            AppIdentity.GetDisplayVersion(),
            document.RootElement.GetProperty("appVersion").GetString());
    }

    [Fact]
    public void Create_IncludesExplicitCategories()
    {
        var config = DefaultGestureConfig.Create();
        config.Categories = ["浏览器"];
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        Assert.Equal("浏览器", document.RootElement.GetProperty("categories")[0].GetString());
    }

    [Fact]
    public void Create_IncludesNormalizedAppearanceTheme()
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings
            {
                Appearance = new AppearanceUiSettings
                {
                    Theme = GestureConfigContract.AppearanceThemes.Dark
                }
            }
        });
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        Assert.Equal(
            GestureConfigContract.AppearanceThemes.Dark,
            document.RootElement
                .GetProperty("uiSettings")
                .GetProperty("appearance")
                .GetProperty("theme")
                .GetString());
    }

    [Fact]
    public void Create_IncludesTitleBarConfiguration()
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings
            {
                Appearance = new AppearanceUiSettings
                {
                    LightTitleBarColor = "#112233",
                    LightTitleBarTextColor = "#445566",
                    DarkTitleBarColor = "#778899",
                    DarkTitleBarTextColor = "#AABBCC"
                }
            }
        });
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));
        var uiSettings = document.RootElement.GetProperty("uiSettings");
        var appearance = uiSettings.GetProperty("appearance");

        Assert.Equal("#112233", appearance.GetProperty("lightTitleBarColor").GetString());
        Assert.Equal("#445566", appearance.GetProperty("lightTitleBarTextColor").GetString());
        Assert.Equal("#778899", appearance.GetProperty("darkTitleBarColor").GetString());
        Assert.Equal("#AABBCC", appearance.GetProperty("darkTitleBarTextColor").GetString());
    }

    [Fact]
    public void Create_IncludesSidebarCollapsedState()
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings
            {
                Sidebar = new SidebarUiSettings { Collapsed = true }
            }
        });
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        Assert.True(
            document.RootElement
                .GetProperty("uiSettings")
                .GetProperty("sidebar")
                .GetProperty("collapsed")
                .GetBoolean());
    }

    [Fact]
    public void Create_IncludesWheelModifierAndTriggerModeForWheelRules()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Scope = GestureConfigContract.Scopes.Global,
                    Pattern = ["Up", "Right"],
                    WheelModifier = GestureConfigContract.WheelDirections.Up,
                    ExecuteOnWheel = true,
                    ActionName = "音量 +",
                    Action = new GestureActionConfig
                    {
                        Type = GestureConfigContract.ActionTypes.Volume,
                        Operation = GestureConfigContract.Operations.Increase,
                        Amount = 5
                    }
                }
            ]
        };
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        var rule = document.RootElement.GetProperty("rules")[0];
        Assert.Equal(GestureConfigContract.WheelDirections.Up, rule.GetProperty("wheelModifier").GetString());
        Assert.True(rule.GetProperty("executeOnWheel").GetBoolean());
        Assert.Equal(["Up", "Right"], rule.GetProperty("pattern").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void Create_KeepsHoldAndScrollRulesWithoutAPattern()
    {
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Scope = GestureConfigContract.Scopes.Global,
                    Pattern = [],
                    WheelModifier = GestureConfigContract.WheelDirections.Down,
                    ExecuteOnWheel = false,
                    ActionName = "静音",
                    Action = new GestureActionConfig
                    {
                        Type = GestureConfigContract.ActionTypes.Volume,
                        Operation = GestureConfigContract.Operations.Mute,
                        Amount = 5
                    }
                }
            ]
        };
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        var rule = document.RootElement.GetProperty("rules")[0];
        Assert.Equal(GestureConfigContract.WheelDirections.Down, rule.GetProperty("wheelModifier").GetString());
        Assert.False(rule.GetProperty("executeOnWheel").GetBoolean());
        Assert.Equal(0, rule.GetProperty("pattern").GetArrayLength());
    }

    [Fact]
    public void Create_IncludesOnboardingState()
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings
            {
                Onboarding = new OnboardingUiSettings { WelcomeCompleted = true }
            }
        });
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        Assert.True(
            document.RootElement
                .GetProperty("uiSettings")
                .GetProperty("onboarding")
                .GetProperty("welcomeCompleted")
                .GetBoolean());
    }

    [Fact]
    public void Create_IncludesDesktopGestureDisableSetting()
    {
        var config = GestureConfigNormalizer.Normalize(new GestureConfig
        {
            UiSettings = new GestureUiSettings
            {
                AppBehavior = new AppBehaviorUiSettings { DisableGesturesOnDesktop = true }
            }
        });
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));

        Assert.True(
            document.RootElement
                .GetProperty("uiSettings")
                .GetProperty("appBehavior")
                .GetProperty("disableGesturesOnDesktop")
                .GetBoolean());
    }
}
