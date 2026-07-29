using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class WebViewRulesPayloadFactoryTests
{
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
}
