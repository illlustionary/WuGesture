using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ConfiguredScopeContextProviderTests
{
    private static GestureApplicationConfig Application(string name, params string[] categories)
    {
        return new GestureApplicationConfig { Name = name, Categories = [.. categories] };
    }

    [Fact]
    public void GetCurrentContext_AttachesTheConfiguredCategories()
    {
        var inner = new FakeScopeProvider(new GestureScopeContext("Code", []));
        var provider = new ConfiguredScopeContextProvider([Application("code.exe", "开发", "编辑器")], inner);

        var context = provider.GetCurrentContext();

        Assert.Equal("Code", context.AppName);
        Assert.Equal(["开发", "编辑器"], context.CategoryNames);
    }

    [Fact]
    public void GetContextForWindow_UsesTheSameMapping()
    {
        var inner = new FakeScopeProvider(new GestureScopeContext("notepad", []));
        var provider = new ConfiguredScopeContextProvider([Application("notepad", "工具")], inner);

        var context = provider.GetContextForWindow((IntPtr)42);

        Assert.Equal(["工具"], context.CategoryNames);
    }

    [Fact]
    public void GetContext_LeavesUnknownApplicationsUntouched()
    {
        var original = new GestureScopeContext("chrome", ["浏览"]);
        var provider = new ConfiguredScopeContextProvider([Application("code", "开发")], new FakeScopeProvider(original));

        var context = provider.GetCurrentContext();

        Assert.Same(original, context);
    }

    [Fact]
    public void GetContext_ReturnsEmptyContextsUnchanged()
    {
        var provider = new ConfiguredScopeContextProvider(
            [Application("code", "开发")],
            new FakeScopeProvider(GestureScopeContext.Empty));

        Assert.Same(GestureScopeContext.Empty, provider.GetCurrentContext());
    }

    [Fact]
    public void BuildAppCategories_TrimsAndDeduplicatesCategories()
    {
        var inner = new FakeScopeProvider(new GestureScopeContext("code", []));
        var provider = new ConfiguredScopeContextProvider(
            [Application(@"C:\Tools\code.exe", " 开发 ", "开发", "", "  ")],
            inner);

        Assert.Equal(["开发"], provider.GetCurrentContext().CategoryNames);
    }

    [Fact]
    public void BuildAppCategories_SkipsApplicationsWithoutUsableIdentityOrCategories()
    {
        var inner = new FakeScopeProvider(new GestureScopeContext("", ["保留"]));
        var provider = new ConfiguredScopeContextProvider(
            [Application("code"), Application(" ", "开发")],
            inner);

        Assert.Equal(["保留"], provider.GetCurrentContext().CategoryNames);
    }

    [Fact]
    public void UpdateApplications_ReplacesThePreviousMapping()
    {
        var inner = new FakeScopeProvider(new GestureScopeContext("code", []));
        var provider = new ConfiguredScopeContextProvider([Application("code", "开发")], inner);

        provider.UpdateApplications([Application("code", "测试")]);

        Assert.Equal(["测试"], provider.GetCurrentContext().CategoryNames);
    }

    [Fact]
    public void BuildAppCategories_LetsTheLastDuplicateWin()
    {
        var inner = new FakeScopeProvider(new GestureScopeContext("code", []));
        var provider = new ConfiguredScopeContextProvider(
            [Application("code", "开发"), Application(@"D:\Portable\code.exe", "便携")],
            inner);

        Assert.Equal(["便携"], provider.GetCurrentContext().CategoryNames);
    }

    private sealed class FakeScopeProvider(GestureScopeContext context) : IGestureScopeContextProvider
    {
        public GestureScopeContext GetCurrentContext() => context;

        public GestureScopeContext GetContextForWindow(IntPtr window) => context;
    }
}
