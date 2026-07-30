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
    public void Create_IncludesHostAppInfoForTheSidebar()
    {
        var config = DefaultGestureConfig.Create();
        var loadedConfig = new LoadedGestureConfig(
            "test.json",
            config,
            GestureConfigMapper.ToRules(config));

        using var document = JsonDocument.Parse(WebViewRulesPayloadFactory.Create(loadedConfig));
        var appInfo = document.RootElement.GetProperty("appInfo");

        Assert.Equal(WebViewMessageTypes.Rules, document.RootElement.GetProperty("type").GetString());
        Assert.Equal(AppIdentity.GetDisplayVersion(), appInfo.GetProperty("version").GetString());
        Assert.True(appInfo.TryGetProperty("icon", out _));
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
}
