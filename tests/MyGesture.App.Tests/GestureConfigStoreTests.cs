using MyGesture.App.GestureEngine;

namespace MyGesture.App.Tests;

public sealed class GestureConfigStoreTests
{
    [Fact]
    public void LoadOrCreate_PreservesConfiguredRulesThatAreNotRuntimeExecutableYet()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MyGestureTests", Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(directory, "gestures.json");
        var store = new GestureConfigStore(configPath);
        var config = new GestureConfig
        {
            Rules =
            [
                new GestureRuleConfig
                {
                    Scope = "global",
                    MouseButton = "middle",
                    Pattern = ["DownLeft"],
                    ActionName = "●↙",
                    Action = new GestureActionConfig
                    {
                        Type = "hotkey",
                        Keys = []
                    }
                }
            ]
        };

        store.Save(config);

        var loaded = store.LoadOrCreate();

        Assert.Single(loaded.Config.Rules);
        Assert.Equal("●↙", loaded.Config.Rules[0].ActionName);
        Assert.Empty(loaded.Rules);
    }
}
