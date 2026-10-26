using System.Reflection;
using System.Text.Json;
using WuGesture.App;
using WuGesture.App.Updater;

namespace WuGesture.App.Tests;

public sealed class UpdateCoordinatorPromptTests
{
    [Fact]
    public void PendingPromptIsDeferredWhileTheWindowIsMinimized()
    {
        var messages = new List<string>();
        var canPrompt = false;
        using var coordinator = CreateCoordinator(messages, () => true, () => canPrompt);
        StageAvailableRelease(coordinator);

        coordinator.PostPendingPrompt();
        Assert.Empty(messages);

        canPrompt = true;
        coordinator.PostPendingPrompt();

        var message = Assert.Single(messages);
        using var document = JsonDocument.Parse(message);
        Assert.Equal("update-available", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("v9.9.9", document.RootElement.GetProperty("latestVersion").GetString());

        // The prompt has been delivered; restoring the window again must not show it twice.
        coordinator.PostPendingPrompt();
        Assert.Single(messages);
    }

    [Fact]
    public void PendingPromptWaitsForTheReloadedWebView()
    {
        var messages = new List<string>();
        var webViewReady = false;
        using var coordinator = CreateCoordinator(messages, () => webViewReady, () => true);
        StageAvailableRelease(coordinator);

        coordinator.PostPendingPrompt();
        Assert.Empty(messages);

        webViewReady = true;
        coordinator.PostPendingPrompt();

        var message = Assert.Single(messages);
        using var document = JsonDocument.Parse(message);
        Assert.Equal("update-available", document.RootElement.GetProperty("type").GetString());
    }

    private static UpdateCoordinator CreateCoordinator(
        List<string> messages,
        Func<bool> isWebViewReady,
        Func<bool> canPrompt)
    {
        return new UpdateCoordinator(
            () => null,
            messages.Add,
            action => action(),
            isWebViewReady,
            canPrompt,
            () => { });
    }

    private static void StageAvailableRelease(UpdateCoordinator coordinator)
    {
        var release = new UpdateReleaseInfo(
            "gitee",
            "v9.9.9",
            "9.9.9",
            "notes",
            "https://example.invalid/releases/v9.9.9",
            null,
            new UpdateAssetInfo("WuGesture-9.9.9.zip", "https://example.invalid/WuGesture-9.9.9.zip", 1024));
        SetField(coordinator, "availableRelease", release);
        SetField(coordinator, "pendingPrompt", true);
    }

    private static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field: {name}");
        field.SetValue(target, value);
    }
}
