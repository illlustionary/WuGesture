using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WuGesture.App.Updater;

/// <summary>
/// Checks the public release feeds, downloads the latest package and hands the extracted files to the
/// updater script. It never touches the running installation itself: the running executable is locked, so
/// only the detached PowerShell helper may replace the files after this process exits.
/// </summary>
internal sealed class UpdateService
{
    private const string ExecutableName = "WuGesture.exe";
    private const string UpdaterScriptFileName = "apply-update.ps1";
    private const string UpdaterScriptResourceName = "WuGesture.App.Updater.apply-update.ps1";
    private const string UpdatesFolderName = "updates";
    private const int Win32ErrorCancelled = 1223;
    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(15);
    private static readonly Regex PackageAssetPattern = new(
        @"^WuGesture-v?\d+(\.\d+){1,3}-windows-x64\.zip$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    // Release notes carry the package checksum, so verifying a download never needs another request.
    private static readonly Regex NotesChecksumPattern = new(
        @"SHA256\s*(?:[(（](?<name>[^)）\r\n]*)[)）])?\s*[:：]\s*(?<hash>[0-9A-Fa-f]{64})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly HttpClient Client = CreateHttpClient();

    public static string GetUpdatesRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppIdentity.AppDataFolderName,
            UpdatesFolderName);
    }

    public static string GetInstallDirectory()
    {
        return Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
    }

    public static Version GetCurrentVersion()
    {
        return typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0);
    }

    public static string GetCurrentDisplayVersion()
    {
        return AppIdentity.GetDisplayVersion();
    }

    public async Task<UpdateCheckResult> CheckAsync(string? preferredSource, CancellationToken cancellationToken)
    {
        var currentVersion = GetCurrentVersion();
        var errors = new List<string>();
        UpdateReleaseInfo? bestRelease = null;
        Version? bestVersion = null;

        // Query the preferred mirror first, but always compare version numbers before offering anything:
        // a mirror that only has an older package must neither hide a newer one nor downgrade the app.
        foreach (var source in EnumerateSources(preferredSource))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var release = await FetchReleaseAsync(source, cancellationToken).ConfigureAwait(false);
                if (release is null)
                {
                    errors.Add($"{source.DisplayName} 上暂未发布可用的安装包");
                    continue;
                }

                if (!TryParseVersion(release.Version, out var releaseVersion))
                {
                    errors.Add($"{source.DisplayName} 的发行版本号无法识别：{release.Version}");
                    continue;
                }

                if (bestVersion is null || releaseVersion > bestVersion)
                {
                    bestRelease = release;
                    bestVersion = releaseVersion;
                }

                // The preferred mirror already offers something newer, so the other mirror cannot win.
                if (bestVersion > currentVersion)
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                AppLogger.Warning(
                    "UpdateService",
                    "check-failed",
                    $"Unable to query {source.DisplayName} for the latest release.",
                    exception);
                errors.Add($"{source.DisplayName}：{exception.Message}");
            }
        }

        if (bestRelease is null || bestVersion is null)
        {
            return new UpdateCheckResult(
                false,
                GetCurrentDisplayVersion(),
                null,
                errors.Count > 0 ? string.Join("；", errors) : "无法获取最新版本信息。");
        }

        return new UpdateCheckResult(
            IsNewerVersion(currentVersion, bestVersion),
            GetCurrentDisplayVersion(),
            bestRelease,
            null);
    }

    public async Task<string> DownloadAsync(
        UpdateReleaseInfo release,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(GetUpdatesRoot(), release.Version);
        Directory.CreateDirectory(directory);
        var packagePath = Path.Combine(directory, release.Asset.Name);

        progress?.Report(new UpdateProgress("downloading", 0, 0, release.Asset.Size));

        using var response = await Client
            .GetAsync(release.Asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? release.Asset.Size;
        var buffer = new byte[81920];
        long receivedBytes = 0;
        var lastPercent = -1;
        var lastReportAt = DateTime.UtcNow;

        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var output = new FileStream(packagePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                receivedBytes += read;
                var percent = totalBytes > 0
                    ? (int)Math.Clamp(receivedBytes * 100 / totalBytes, 0, 100)
                    : 0;
                if (percent != lastPercent && (percent == 100 || (DateTime.UtcNow - lastReportAt).TotalMilliseconds >= 150))
                {
                    lastPercent = percent;
                    lastReportAt = DateTime.UtcNow;
                    progress?.Report(new UpdateProgress("downloading", percent, receivedBytes, totalBytes));
                }
            }
        }

        if (totalBytes > 0 && receivedBytes != totalBytes)
        {
            TryDeleteFile(packagePath);
            throw new InvalidDataException($"更新包下载不完整（{receivedBytes}/{totalBytes} 字节）。");
        }

        progress?.Report(new UpdateProgress("verifying", 100, receivedBytes, totalBytes));
        await VerifyChecksumAsync(release, packagePath, cancellationToken).ConfigureAwait(false);
        return packagePath;
    }

    public Task<string> PrepareAsync(
        string packagePath,
        string version,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => Prepare(packagePath, version, progress, cancellationToken), cancellationToken);
    }

    public bool CanReplaceInstallDirectory()
    {
        try
        {
            var installDirectory = GetInstallDirectory();
            var parentDirectory = Directory.GetParent(installDirectory)?.FullName;
            if (parentDirectory is null)
            {
                return false;
            }

            foreach (var directory in new[] { installDirectory, parentDirectory })
            {
                var probePath = Path.Combine(directory, $".wugesture-update-test-{Guid.NewGuid():N}");
                using (File.Create(probePath))
                {
                }

                File.Delete(probePath);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void StartApply(
        string stagingAppRoot,
        string version,
        string? launchArguments,
        bool elevated)
    {
        var installDirectory = GetInstallDirectory();
        var parentDirectory = Directory.GetParent(installDirectory)?.FullName
            ?? throw new InvalidOperationException("无法解析 WuGesture 的安装目录。");
        var backupDirectory = Path.Combine(
            parentDirectory,
            $"{Path.GetFileName(installDirectory)}.old-{DateTime.Now:yyyyMMddHHmmss}");
        var workDirectory = Path.Combine(GetUpdatesRoot(), version);
        Directory.CreateDirectory(workDirectory);

        var scriptPath = Path.Combine(workDirectory, UpdaterScriptFileName);
        ExtractUpdaterScript(scriptPath);
        var logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppIdentity.AppDataFolderName,
            "logs");
        Directory.CreateDirectory(logsDirectory);
        var logPath = Path.Combine(logsDirectory, $"update-{DateTime.Now:yyyyMMddHHmmss}.log");
        var arguments = string.Join(
            ' ',
            new[]
            {
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-WindowStyle", "Hidden",
                "-File", scriptPath,
                "-ProcessId", Environment.ProcessId.ToString(CultureInfo.InvariantCulture),
                "-InstallDir", installDirectory,
                "-StagingDir", stagingAppRoot,
                "-BackupDir", backupDirectory,
                "-LaunchArguments", launchArguments ?? "",
                "-LogPath", logPath
            }.Select(QuoteArgument));

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            WorkingDirectory = workDirectory,
            UseShellExecute = elevated,
            CreateNoWindow = !elevated,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        if (elevated)
        {
            startInfo.Verb = "runas";
        }

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("无法启动更新程序。");
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == Win32ErrorCancelled)
        {
            throw new OperationCanceledException("已取消管理员授权，更新没有开始。");
        }

        AppLogger.Information(
            "UpdateService",
            "updater-started",
            $"Started the updater for {version} (elevated: {elevated}).");
    }

    public static void CleanupStaleFiles()
    {
        try
        {
            var updatesRoot = GetUpdatesRoot();
            if (Directory.Exists(updatesRoot))
            {
                var updateCutoff = DateTime.UtcNow.AddDays(-3);
                foreach (var directory in Directory.GetDirectories(updatesRoot))
                {
                    TryDeleteDirectory(directory, updateCutoff);
                }
            }

            var installDirectory = GetInstallDirectory();
            var parentDirectory = Directory.GetParent(installDirectory);
            if (parentDirectory is not { Exists: true })
            {
                return;
            }

            var backupCutoff = DateTime.UtcNow.AddDays(-1);
            var backupPrefix = $"{Path.GetFileName(installDirectory)}.old-";
            foreach (var directory in Directory.GetDirectories(parentDirectory.FullName, $"{backupPrefix}*"))
            {
                TryDeleteDirectory(directory, backupCutoff);
            }

            var logsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppIdentity.AppDataFolderName,
                "logs");
            if (Directory.Exists(logsDirectory))
            {
                var logCutoff = DateTime.UtcNow.AddDays(-7);
                foreach (var file in Directory.GetFiles(logsDirectory, "update-*.log"))
                {
                    if (File.GetLastWriteTimeUtc(file) < logCutoff)
                    {
                        TryDeleteFile(file);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            AppLogger.Warning("UpdateService", "cleanup-failed", "Unable to clean up previous update files.", exception);
        }
    }

    internal static bool TryParseVersion(string? tagName, out Version version)
    {
        version = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        var text = tagName.Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
        {
            text = text[1..];
        }

        var metadataIndex = text.IndexOf('+');
        if (metadataIndex >= 0)
        {
            text = text[..metadataIndex];
        }

        // Prerelease tags (v1.4.0-beta.1) are not offered as updates.
        if (text.Contains('-'))
        {
            return false;
        }

        return Version.TryParse(text, out version!);
    }

    internal static bool IsNewerVersion(Version current, Version latest)
    {
        // Assembly versions always carry a revision (1.3.10.0) while release tags may stop at the build
        // number (v1.3.10), so compare with explicit zeros instead of treating "missing" as lower.
        return NormalizeVersion(latest) > NormalizeVersion(current);
    }

    private static Version NormalizeVersion(Version version)
    {
        return new Version(
            version.Major,
            version.Minor,
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0));
    }

    internal static bool IsSafeArchivePath(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
        {
            return false;
        }

        var normalized = entryName.Replace('\\', '/');
        return !Path.IsPathRooted(normalized) &&
            !normalized.StartsWith('/') &&
            !normalized.Contains(':') &&
            !normalized.Split('/').Any(segment => segment == "..");
    }

    internal static string? ParseChecksumFromNotes(string? notes, string assetName)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        string? genericHash = null;
        foreach (Match match in NotesChecksumPattern.Matches(notes))
        {
            var declaredAssetName = match.Groups["name"].Value.Trim();
            if (declaredAssetName.Length == 0)
            {
                genericHash ??= match.Groups["hash"].Value;
                continue;
            }

            if (string.Equals(declaredAssetName, assetName, StringComparison.OrdinalIgnoreCase))
            {
                return match.Groups["hash"].Value;
            }
        }

        return genericHash;
    }

    internal static UpdateAssetInfo? SelectPackageAsset(IEnumerable<UpdateAssetInfo> assets)
    {
        return assets.FirstOrDefault(asset =>
            PackageAssetPattern.IsMatch(asset.Name) &&
            !string.IsNullOrWhiteSpace(asset.DownloadUrl));
    }

    private static IEnumerable<UpdateSourceDefinition> EnumerateSources(string? preferredSource)
    {
        var first = UpdateSources.Get(preferredSource);
        yield return first;
        yield return UpdateSources.Get(first.Id == UpdateSources.GitHub ? UpdateSources.Gitee : UpdateSources.GitHub);
    }

    private static async Task<UpdateReleaseInfo?> FetchReleaseAsync(
        UpdateSourceDefinition source,
        CancellationToken cancellationToken)
    {
        var latest = await GetJsonAsync($"{source.ApiBaseUrl}/releases/latest", cancellationToken)
            .ConfigureAwait(false);
        if (latest is { } latestElement)
        {
            var release = ParseRelease(latestElement, source);
            if (release is not null)
            {
                return release;
            }
        }

        var listUrl = source.Id == UpdateSources.GitHub
            ? $"{source.ApiBaseUrl}/releases?per_page=20"
            : $"{source.ApiBaseUrl}/releases?page=1&per_page=20";
        var list = await GetJsonAsync(listUrl, cancellationToken).ConfigureAwait(false);
        if (list is not { ValueKind: JsonValueKind.Array } releases)
        {
            return null;
        }

        UpdateReleaseInfo? best = null;
        foreach (var element in releases.EnumerateArray())
        {
            var release = ParseRelease(element, source);
            if (release is null)
            {
                continue;
            }

            if (best is null ||
                (TryParseVersion(release.Version, out var candidate) &&
                 TryParseVersion(best.Version, out var current) &&
                 IsNewerVersion(current, candidate)))
            {
                best = release;
            }
        }

        return best;
    }

    private static async Task<JsonElement?> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("application/json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ApiTimeout);
        using var response = await Client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token)
            .ConfigureAwait(false);
        return document.RootElement.Clone();
    }

    private static UpdateReleaseInfo? ParseRelease(JsonElement element, UpdateSourceDefinition source)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (GetBoolean(element, "draft") || GetBoolean(element, "prerelease"))
        {
            return null;
        }

        var tagName = GetString(element, "tag_name") ?? GetString(element, "name");
        if (!TryParseVersion(tagName, out var version))
        {
            return null;
        }

        if (!element.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var assetInfos = new List<UpdateAssetInfo>();
        foreach (var asset in assets.EnumerateArray())
        {
            var name = GetString(asset, "name");
            var url = GetString(asset, "browser_download_url") ?? GetString(asset, "download_url");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            assetInfos.Add(new UpdateAssetInfo(name, url, GetInt64(asset, "size")));
        }

        var package = SelectPackageAsset(assetInfos);
        if (package is null)
        {
            return null;
        }

        var notes = GetString(element, "body") ?? "";
        var htmlUrl = GetString(element, "html_url") ?? $"{source.ReleasePageUrl}/tag/{tagName}";
        DateTimeOffset? publishedAt = null;
        var publishedText = GetString(element, "published_at") ?? GetString(element, "created_at");
        if (DateTimeOffset.TryParse(
            publishedText,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsedPublishedAt))
        {
            publishedAt = parsedPublishedAt;
        }

        return new UpdateReleaseInfo(
            source.Id,
            tagName!,
            version.ToString(),
            notes,
            htmlUrl,
            publishedAt,
            package);
    }

    private static string Prepare(
        string packagePath,
        string version,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stagingRoot = Path.Combine(GetUpdatesRoot(), version, "staged");
        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, recursive: true);
        }

        Directory.CreateDirectory(stagingRoot);
        using (var archive = ZipFile.OpenRead(packagePath))
        {
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsSafeArchivePath(entry.FullName))
                {
                    throw new InvalidDataException($"更新包中包含不安全的路径：{entry.FullName}");
                }
            }

            archive.ExtractToDirectory(stagingRoot, overwriteFiles: true);
        }

        progress?.Report(new UpdateProgress("extracting", 100, 0, 0));
        var appRoot = FindApplicationRoot(stagingRoot);
        if (appRoot is null)
        {
            throw new InvalidDataException("更新包中未找到 WuGesture.exe。");
        }

        if (!File.Exists(Path.Combine(appRoot, "Web", "index.html")))
        {
            throw new InvalidDataException("更新包中缺少 Web 配置界面文件。");
        }

        return appRoot;
    }

    private static string? FindApplicationRoot(string stagingRoot)
    {
        var nestedRoot = Path.Combine(stagingRoot, "WuGesture");
        if (File.Exists(Path.Combine(nestedRoot, ExecutableName)))
        {
            return nestedRoot;
        }

        return File.Exists(Path.Combine(stagingRoot, ExecutableName)) ? stagingRoot : null;
    }

    private static async Task VerifyChecksumAsync(
        UpdateReleaseInfo release,
        string packagePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var expectedHash = ParseChecksumFromNotes(release.Notes, release.Asset.Name);
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                AppLogger.Warning(
                    "UpdateService",
                    "checksum-missing",
                    $"The release notes do not declare a SHA256 checksum for {release.Asset.Name}.");
                return;
            }

            await using var stream = File.OpenRead(packagePath);
            var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteFile(packagePath);
                throw new InvalidDataException("更新包完整性校验失败（SHA256 不匹配）。");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not InvalidDataException)
        {
            AppLogger.Warning(
                "UpdateService",
                "checksum-verification-failed",
                "Unable to verify the downloaded update package checksum.",
                exception);
        }
    }

    private static void ExtractUpdaterScript(string scriptPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(UpdaterScriptResourceName)
            ?? throw new InvalidOperationException("内置更新脚本缺失。");
        using var output = File.Create(scriptPath);
        stream.CopyTo(output);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"WuGesture/{GetCurrentDisplayVersion()}");
        return client;
    }

    private static string QuoteArgument(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static long GetInt64(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var number)
            ? number
            : 0;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path, DateTime cutoffUtc)
    {
        try
        {
            if (Directory.GetLastWriteTimeUtc(path) < cutoffUtc)
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }
}
