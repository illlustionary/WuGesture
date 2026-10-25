namespace WuGesture.App.Updater;

internal sealed record UpdateAssetInfo(string Name, string DownloadUrl, long Size);

internal sealed record UpdateReleaseInfo(
    string Source,
    string TagName,
    string Version,
    string Notes,
    string HtmlUrl,
    DateTimeOffset? PublishedAt,
    UpdateAssetInfo Asset,
    UpdateAssetInfo? Checksums);

internal sealed record UpdateCheckResult(
    bool Available,
    string CurrentVersion,
    UpdateReleaseInfo? Release,
    string? Error);

internal sealed record UpdateProgress(
    string Phase,
    int Percent,
    long ReceivedBytes,
    long TotalBytes);
