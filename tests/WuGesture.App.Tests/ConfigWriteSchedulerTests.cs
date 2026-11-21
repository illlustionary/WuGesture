using WuGesture.App;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ConfigWriteSchedulerTests : IDisposable
{
    private readonly string directory =
        Path.Combine(Path.GetTempPath(), $"wugesture-writer-{Guid.NewGuid():N}");

    private readonly string configPath;

    public ConfigWriteSchedulerTests()
    {
        Directory.CreateDirectory(directory);
        configPath = Path.Combine(directory, ConfigStorageContract.ConfigFileName);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Flush_WritesTheLastQueuedConfiguration()
    {
        var store = new GestureConfigStore(configPath);
        using var scheduler = new ConfigWriteScheduler(store, action => action(), (_, _) => { });

        scheduler.Enqueue(WithActionName(DefaultGestureConfig.Create(), "first"));
        scheduler.Enqueue(WithActionName(DefaultGestureConfig.Create(), "second"));

        Assert.True(scheduler.Flush(TimeSpan.FromSeconds(5)));

        var written = File.ReadAllText(configPath);
        Assert.Contains("second", written);
        Assert.DoesNotContain("first", written);
    }

    [Fact]
    public void Dispose_WritesThePendingConfiguration()
    {
        var store = new GestureConfigStore(configPath);
        var scheduler = new ConfigWriteScheduler(store, action => action(), (_, _) => { });

        scheduler.Enqueue(WithActionName(DefaultGestureConfig.Create(), "pending"));
        scheduler.Dispose();

        Assert.Contains("pending", File.ReadAllText(configPath));
    }

    private static GestureConfig WithActionName(GestureConfig config, string actionName)
    {
        config.Rules[0].ActionName = actionName;
        return config;
    }
}
