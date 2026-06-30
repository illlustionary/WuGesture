using MyGesture.App.GestureEngine;

namespace MyGesture.App.Tests;

public sealed class ConfiguredScopeContextProviderTests
{
    [Fact]
    public void GetCurrentContext_MapsAppNameToConfiguredCategory()
    {
        var provider = new ConfiguredScopeContextProvider(
        [
            new GestureApplicationConfig
            {
                Name = "msedge",
                Category = "Browser"
            }
        ],
        new StubScopeContextProvider(new GestureScopeContext("msedge", "")));

        var context = provider.GetCurrentContext();

        Assert.Equal("msedge", context.AppName);
        Assert.Equal("Browser", context.CategoryName);
    }

    [Fact]
    public void GetCurrentContext_NormalizesExeNameAndMatchesCaseInsensitive()
    {
        var provider = new ConfiguredScopeContextProvider(
        [
            new GestureApplicationConfig
            {
                Name = "MSEDGE.EXE",
                Category = "Browser"
            }
        ],
        new StubScopeContextProvider(new GestureScopeContext("msedge", "")));

        var context = provider.GetCurrentContext();

        Assert.Equal("Browser", context.CategoryName);
    }

    [Fact]
    public void UpdateApplications_ReplacesCategoryMap()
    {
        var provider = new ConfiguredScopeContextProvider(
        [
            new GestureApplicationConfig
            {
                Name = "msedge",
                Category = "Browser"
            }
        ],
        new StubScopeContextProvider(new GestureScopeContext("msedge", "")));

        provider.UpdateApplications(
        [
            new GestureApplicationConfig
            {
                Name = "msedge",
                Category = "Work"
            }
        ]);

        var context = provider.GetCurrentContext();

        Assert.Equal("Work", context.CategoryName);
    }

    private sealed class StubScopeContextProvider(GestureScopeContext context) : IGestureScopeContextProvider
    {
        public GestureScopeContext GetCurrentContext()
        {
            return context;
        }
    }
}
