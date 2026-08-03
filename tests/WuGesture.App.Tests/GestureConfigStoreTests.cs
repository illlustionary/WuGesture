using System.Text.Json;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureConfigStoreTests
{
    [Fact]
    public void DefaultConfig_ContainsOnlyGlobalRulesWithoutApplicationsOrCategories()
    {
        var config = DefaultGestureConfig.Create();

        Assert.Empty(config.Applications);
        Assert.Empty(config.Categories);
        Assert.All(config.Rules, rule => Assert.Equal(GestureConfigContract.Scopes.Global, rule.Scope));
        Assert.DoesNotContain(config.Rules, rule => rule.ActionName is "剪切板" or "翻译");
    }

    [Fact]
    public void LoadOrCreate_BackupsAndMigratesLegacyConfig()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "gestures.json");
        const string legacyJson = "{\"rules\":[{\"actionName\":\"保留的手势\"}],\"uiSettings\":{\"gestureHint\":{\"displayDurationMs\":\"invalid\"}}}";
        File.WriteAllText(path, legacyJson);

        var loaded = new GestureConfigStore(path).LoadOrCreate();

        Assert.Equal(GestureConfigContract.Schema.CurrentVersion, loaded.Config.SchemaVersion);
        Assert.Equal("保留的手势", loaded.Config.Rules[0].ActionName);
        Assert.Equal(300, loaded.Config.UiSettings.GestureHint.DisplayDurationMs);
        Assert.Equal(10, loaded.Config.UiSettings.GestureHint.WidthPercent);
        Assert.True(loaded.Config.UiSettings.GestureHint.AutoWidth);
        var backups = Directory.GetFiles(directory.Path, "gestures.backup-*.json");
        Assert.Single(backups);
        Assert.Equal(legacyJson, File.ReadAllText(backups[0]));

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(
            GestureConfigContract.Schema.CurrentVersion,
            document.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public void LoadOrCreate_BackupsCorruptConfigAndWritesCurrentDefaults()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "gestures.json");
        const string corruptJson = "{\"schemaVersion\":1";
        File.WriteAllText(path, corruptJson);

        var loaded = new GestureConfigStore(path).LoadOrCreate();

        Assert.Equal(GestureConfigContract.Schema.CurrentVersion, loaded.Config.SchemaVersion);
        var backups = Directory.GetFiles(directory.Path, "gestures.backup-*.json");
        Assert.Single(backups);
        Assert.Equal(corruptJson, File.ReadAllText(backups[0]));
    }

    [Fact]
    public void LoadOrCreate_RejectsFutureConfigWithoutChangingIt()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "gestures.json");
        const string futureJson = "{\"schemaVersion\":2,\"rules\":[]}";
        File.WriteAllText(path, futureJson);

        var exception = Assert.Throws<InvalidOperationException>(() => new GestureConfigStore(path).LoadOrCreate());

        Assert.Contains("高于当前支持版本", exception.Message);
        Assert.Equal(futureJson, File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(directory.Path, "gestures.backup-*.json"));
    }

    [Fact]
    public void LoadOrCreate_PreservesCurrentConfigWithoutCreatingABackup()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "gestures.json");
        var store = new GestureConfigStore(path);
        var config = DefaultGestureConfig.Create();
        config.Rules[0].ActionName = "保留的手势";
        store.SaveAndLoad(config);

        var loaded = store.LoadOrCreate();

        Assert.Equal("保留的手势", loaded.Config.Rules[0].ActionName);
        Assert.Empty(Directory.GetFiles(directory.Path, "gestures.backup-*.json"));
    }

    [Fact]
    public void SaveAndLoad_PersistsCurrentSchemaVersion()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "gestures.json");
        var store = new GestureConfigStore(path);

        store.SaveAndLoad(DefaultGestureConfig.Create());

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(
            GestureConfigContract.Schema.CurrentVersion,
            document.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public void SaveJsonAndLoad_MigratesLegacyImport()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "gestures.json");
        var store = new GestureConfigStore(path);
        store.SaveAndLoad(DefaultGestureConfig.Create());
        var loaded = store.SaveJsonAndLoad("{\"rules\":[{\"actionName\":\"导入的手势\"}]}");

        Assert.Equal(GestureConfigContract.Schema.CurrentVersion, loaded.Config.SchemaVersion);
        Assert.Equal("导入的手势", loaded.Config.Rules[0].ActionName);
        Assert.Empty(Directory.GetFiles(directory.Path, "gestures.backup-*.json"));
    }

    [Fact]
    public void SaveJsonAndLoad_RejectsFutureImportWithoutTouchingLocalConfig()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "gestures.json");
        var store = new GestureConfigStore(path);
        store.SaveAndLoad(DefaultGestureConfig.Create());
        var originalJson = File.ReadAllText(path);

        Assert.Throws<InvalidOperationException>(() => store.SaveJsonAndLoad("{\"schemaVersion\":2}"));

        Assert.Equal(originalJson, File.ReadAllText(path));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"WuGestureTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
