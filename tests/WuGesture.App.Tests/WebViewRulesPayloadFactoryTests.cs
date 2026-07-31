using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class WebViewRulesPayloadFactoryTests
{
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
}
