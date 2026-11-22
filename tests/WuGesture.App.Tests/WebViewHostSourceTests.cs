using WuGesture.App;

namespace WuGesture.App.Tests;

public sealed class WebViewHostSourceTests
{
    [Theory]
    [InlineData("https://appassets.wugesture.invalid/index.html", true)]
    [InlineData("https://APPASSETS.WUGESTURE.INVALID/index.html#/settings/app-behavior", true)]
    [InlineData("https://evil.example/index.html", false)]
    [InlineData("http://appassets.wugesture.invalid/index.html", false)]
    [InlineData("https://appassets.wugesture.invalid.evil.example/index.html", false)]
    [InlineData("https://gesture.wu.philosophy/index.html", false)]
    [InlineData("file:///C:/Windows/Temp/message.html", false)]
    [InlineData("about:blank", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsTrustedSource_OnlyAcceptsTheMappedConfigurationHost(string? source, bool expected)
    {
        Assert.Equal(expected, WebViewHost.IsTrustedSource(source));
    }
}
