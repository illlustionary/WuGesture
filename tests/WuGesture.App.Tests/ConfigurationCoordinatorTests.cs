using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ConfigurationCoordinatorTests
{
    [Fact]
    public void CreateConfig_MapsEverySectionFromTheEditorMessage()
    {
        const string json = """
            {
              "type": "save-rules",
              "rules": [
                {
                  "scope": "global",
                  "mouseButton": "right",
                  "pattern": ["Left"],
                  "actionName": "返回",
                  "action": { "type": "hotkey", "keys": ["Alt", "Left"], "operation": "", "amount": 5, "path": "", "arguments": [] }
                }
              ],
              "applications": [
                { "name": "notepad", "displayName": "记事本", "path": "C:\\Windows\\notepad.exe", "disableEdgeActions": false }
              ],
              "categories": ["浏览器"],
              "edgeActions": [
                {
                  "enabled": true,
                  "triggerType": "corner",
                  "location": "top-left",
                  "wheelDirection": "",
                  "frictionCount": 4,
                  "action": { "type": "hotkey", "keys": ["Win", "Tab"], "operation": "", "amount": 5, "path": "", "arguments": [] }
                }
              ],
              "uiSettings": { "appearance": { "theme": "dark" } }
            }
            """;

        var config = ConfigurationCoordinator.CreateConfig(json, new GestureUiSettings());

        Assert.Equal(GestureConfigContract.Schema.CurrentVersion, config.SchemaVersion);
        Assert.Single(config.Rules);
        Assert.Equal(["Left"], config.Rules[0].Pattern);
        Assert.Single(config.Applications);
        Assert.Equal("notepad", config.Applications[0].Name);
        Assert.Equal(["浏览器"], config.Categories);
        Assert.Single(config.EdgeActions);
        Assert.Equal("top-left", config.EdgeActions[0].Location);
        Assert.Equal(GestureConfigContract.AppearanceThemes.Dark, config.UiSettings.Appearance.Theme);
    }

    [Fact]
    public void CreateConfig_TreatsMissingSectionsAsEmptyAndKeepsFallbackUiSettings()
    {
        var fallback = new GestureUiSettings
        {
            Appearance = new AppearanceUiSettings { Theme = GestureConfigContract.AppearanceThemes.Light }
        };

        var config = ConfigurationCoordinator.CreateConfig("""{"type":"save-rules"}""", fallback);

        Assert.Empty(config.Rules);
        Assert.Empty(config.Applications);
        Assert.Empty(config.Categories);
        Assert.Empty(config.EdgeActions);
        Assert.Same(fallback, config.UiSettings);
    }

    [Fact]
    public void CreateConfig_UsesDefaultUiSettingsWhenTheMessageAndFallbackAreMissing()
    {
        var config = ConfigurationCoordinator.CreateConfig("""{"type":"save-rules"}""", null);

        Assert.NotNull(config.UiSettings);
        Assert.NotNull(config.UiSettings.AppBehavior);
    }
}
