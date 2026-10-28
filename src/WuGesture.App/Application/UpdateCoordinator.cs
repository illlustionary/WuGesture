using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using WuGesture.App.GestureEngine;
using WuGesture.App.Updater;

namespace WuGesture.App;

/// <summary>
/// Coordinates update checks with the WebView settings UI. All messages are posted on the UI thread; the
/// network and file work runs in the background so startup and gesture handling are never blocked.
/// </summary>
internal sealed class UpdateCoordinator : IDisposable
{
    private static readonly TimeSpan AutoCheckDelay = TimeSpan.FromSeconds(2);
    private const int MaximumNotesLength = 8000;

    private readonly UpdateService updateService = new();
    private readonly Func<UpdateUiSettings?> getSettings;
    private readonly Action<string> postMessage;
    private readonly Action<Action> dispatch;
    private readonly Func<bool> isWebViewReady;
    private readonly Func<bool> canPrompt;
    private readonly Action exitForUpdate;

    private CancellationTokenSource? checkCancellation;
    private CancellationTokenSource? downloadCancellation;
    private UpdateReleaseInfo? availableRelease;
    private string? promptedVersion;
    private int isChecking;
    private int pendingManualCheck;
    private bool isApplying;
    private bool autoCheckStarted;
    private bool pendingPrompt;

    public UpdateCoordinator(
        Func<UpdateUiSettings?> getSettings,
        Action<string> postMessage,
        Action<Action> dispatch,
        Func<bool> isWebViewReady,
        Func<bool> canPrompt,
        Action exitForUpdate)
    {
        this.getSettings = getSettings;
        this.postMessage = postMessage;
        this.dispatch = dispatch;
        this.isWebViewReady = isWebViewReady;
        this.canPrompt = canPrompt;
        this.exitForUpdate = exitForUpdate;
    }

    public void StartAutoCheck()
    {
        if (autoCheckStarted)
        {
            return;
        }

        autoCheckStarted = true;
        UpdateService.CleanupStaleFiles();
        if (getSettings()?.CheckForUpdates != true)
        {
            AppLogger.Information("UpdateCoordinator", "auto-check-disabled", "Automatic update checks are disabled in the settings.");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(AutoCheckDelay).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            await CheckAsync(manual: false).ConfigureAwait(false);
        });
    }

    public void CheckForUpdate(bool manual)
    {
        _ = CheckAsync(manual);
    }

    public void StartUpdate()
    {
        if (isApplying)
        {
            return;
        }

        if (availableRelease is null)
        {
            dispatch(() => PostFailed("没有可用的更新版本，请重新检查更新。"));
            return;
        }

        // Never replace a newer installation with an older package, even if the release feed goes backwards.
        if (!UpdateService.TryParseVersion(availableRelease.Version, out var releaseVersion) ||
            !UpdateService.IsNewerVersion(UpdateService.GetCurrentVersion(), releaseVersion))
        {
            dispatch(() => PostFailed("更新包版本不高于当前版本，已取消更新。"));
            return;
        }

        isApplying = true;
        var release = availableRelease;
        var cancellation = new CancellationTokenSource();
        downloadCancellation?.Dispose();
        downloadCancellation = cancellation;
        _ = Task.Run(async () =>
        {
            try
            {
                var progress = new Progress<UpdateProgress>(value => dispatch(() => PostProgress(value)));
                var packagePath = await updateService
                    .DownloadAsync(release, progress, cancellation.Token)
                    .ConfigureAwait(false);
                var stagingAppRoot = await updateService
                    .PrepareAsync(packagePath, release.Version, progress, cancellation.Token)
                    .ConfigureAwait(false);
                var elevated = !updateService.CanReplaceInstallDirectory();
                if (elevated)
                {
                    dispatch(() => PostProgress(new UpdateProgress("elevating", 100, 0, 0)));
                }

                updateService.StartApply(stagingAppRoot, release.Version, BuildLaunchArguments(), elevated);
                dispatch(() =>
                {
                    isApplying = false;
                    PostRestarting();
                    exitForUpdate();
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // The user pressed cancel in the dialog; the frontend has already reset to "available".
                AppLogger.Information("UpdateCoordinator", "update-cancelled", "The update was cancelled before it was applied.");
                dispatch(() => isApplying = false);
            }
            catch (OperationCanceledException exception)
            {
                // Cancelled outside our token: the UAC prompt was dismissed, or the download timed out.
                AppLogger.Warning("UpdateCoordinator", "update-interrupted", "The update was interrupted.", exception);
                dispatch(() =>
                {
                    isApplying = false;
                    PostFailed(exception.Message);
                });
            }
            catch (Exception exception)
            {
                AppLogger.Error("UpdateCoordinator", "update-failed", "Unable to download or apply the update.", exception);
                dispatch(() =>
                {
                    isApplying = false;
                    PostFailed($"更新失败：{exception.Message}");
                });
            }
        });
    }

    public void Cancel()
    {
        downloadCancellation?.Cancel();
    }

    public void PostPendingPrompt()
    {
        if (!pendingPrompt || availableRelease is null)
        {
            return;
        }

        TryPostPrompt(manual: false);
    }

    public void OpenReleasePage()
    {
        var url = availableRelease?.HtmlUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            url = UpdateSources.Get(getSettings()?.Source).ReleasePageUrl;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            AppLogger.Warning("UpdateCoordinator", "open-release-page-failed", "Unable to open the release page.", exception);
        }
    }

    public void Dispose()
    {
        checkCancellation?.Cancel();
        checkCancellation?.Dispose();
        checkCancellation = null;
        downloadCancellation?.Cancel();
        downloadCancellation?.Dispose();
        downloadCancellation = null;
    }

    private async Task CheckAsync(bool manual)
    {
        if (Interlocked.Exchange(ref isChecking, 1) != 0)
        {
            if (manual)
            {
                Interlocked.Exchange(ref pendingManualCheck, 1);
            }

            return;
        }

        try
        {
            checkCancellation?.Dispose();
            checkCancellation = new CancellationTokenSource();
            var source = getSettings()?.Source ?? UpdateSources.Gitee;
            var result = await updateService
                .CheckAsync(source, checkCancellation.Token)
                .ConfigureAwait(false);
            dispatch(() => HandleCheckResult(result, manual));
        }
        catch (OperationCanceledException)
        {
            if (manual)
            {
                dispatch(() => PostError("检查更新已取消。"));
            }
        }
        catch (Exception exception)
        {
            AppLogger.Warning("UpdateCoordinator", "check-failed", "Unable to check for updates.", exception);
            if (manual)
            {
                dispatch(() => PostError($"检查更新失败：{exception.Message}"));
            }
        }
        finally
        {
            Interlocked.Exchange(ref isChecking, 0);
            if (Interlocked.Exchange(ref pendingManualCheck, 0) != 0)
            {
                CheckForUpdate(manual: true);
            }
        }
    }

    private void HandleCheckResult(UpdateCheckResult result, bool manual)
    {
        if (result.Error is not null)
        {
            AppLogger.Warning("UpdateCoordinator", "check-unavailable", result.Error);
            if (manual)
            {
                PostError(result.Error);
            }

            return;
        }

        if (result.Available && result.Release is not null)
        {
            AppLogger.Information(
                "UpdateCoordinator",
                "update-available",
                $"Version {result.Release.TagName} is available from {result.Release.Source}.");
            availableRelease = result.Release;
            pendingPrompt = true;
            TryPostPrompt(manual);
            return;
        }

        AppLogger.Information("UpdateCoordinator", "up-to-date", "The installed version is up to date.");
        if (manual)
        {
            PostNone(result.CurrentVersion);
        }
    }

    private void TryPostPrompt(bool manual)
    {
        if (availableRelease is null)
        {
            return;
        }

        var version = availableRelease.Version;
        if (!isWebViewReady())
        {
            if (!manual)
            {
                DeferPrompt("the configuration UI is not ready yet");
            }

            return;
        }

        if (!manual)
        {
            if (string.Equals(promptedVersion, version, StringComparison.Ordinal))
            {
                return;
            }

            if (!canPrompt())
            {
                DeferPrompt("the configuration window is hidden or minimized");
                return;
            }
        }

        PostAvailable(availableRelease, manual);
        promptedVersion = version;
        pendingPrompt = false;
        AppLogger.Information(
            "UpdateCoordinator",
            "update-prompt-posted",
            $"Showing update {version} in the configuration UI.");
    }

    private void DeferPrompt(string reason)
    {
        if (!pendingPrompt)
        {
            AppLogger.Information(
                "UpdateCoordinator",
                "update-prompt-deferred",
                $"Keeping update {availableRelease?.Version} for later because {reason}.");
        }

        pendingPrompt = true;
    }

    private void PostAvailable(UpdateReleaseInfo release, bool manual)
    {
        PostMessage(new
        {
            type = WebViewMessageTypes.UpdateAvailable,
            currentVersion = UpdateService.GetCurrentDisplayVersion(),
            latestVersion = release.TagName,
            source = release.Source,
            preferredSource = getSettings()?.Source ?? UpdateSources.Gitee,
            notes = TruncateNotes(release.Notes),
            publishedAt = release.PublishedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            htmlUrl = release.HtmlUrl,
            assetName = release.Asset.Name,
            assetSize = release.Asset.Size,
            manual
        });
    }

    private void PostNone(string currentVersion)
    {
        PostMessage(new
        {
            type = WebViewMessageTypes.UpdateNone,
            currentVersion
        });
    }

    private void PostError(string message)
    {
        PostMessage(new
        {
            type = WebViewMessageTypes.UpdateError,
            message
        });
    }

    private void PostProgress(UpdateProgress progress)
    {
        PostMessage(new
        {
            type = WebViewMessageTypes.UpdateProgress,
            phase = progress.Phase,
            percent = progress.Percent,
            receivedBytes = progress.ReceivedBytes,
            totalBytes = progress.TotalBytes
        });
    }

    private void PostFailed(string message)
    {
        PostMessage(new
        {
            type = WebViewMessageTypes.UpdateFailed,
            message
        });
    }

    private void PostRestarting()
    {
        PostMessage(new
        {
            type = WebViewMessageTypes.UpdateRestarting,
            message = "更新已准备就绪，WuGesture 正在重启。"
        });
    }

    private void PostMessage(object payload)
    {
        postMessage(JsonSerializer.Serialize(payload));
    }

    private static string TruncateNotes(string notes)
    {
        var normalized = (notes ?? "").Replace("\r\n", "\n").Trim();
        return normalized.Length <= MaximumNotesLength
            ? normalized
            : $"{normalized[..MaximumNotesLength]}\n…";
    }

    private static string BuildLaunchArguments()
    {
        // The updater script appends --updated only when the new version is actually in place, so a rollback
        // restart keeps the previous behaviour without claiming a successful update.
        var arguments = Environment
            .GetCommandLineArgs()
            .Skip(1)
            .Where(argument =>
                !string.Equals(argument, AppIdentity.ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(argument, AppIdentity.UpdateRestartArgument, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return string.Join(' ', arguments);
    }
}
