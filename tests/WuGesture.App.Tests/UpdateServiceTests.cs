using WuGesture.App.Updater;

namespace WuGesture.App.Tests;

public sealed class UpdateServiceTests
{
    private const string HashA = "3f2a9c1e5b7d4068a1c2e3f4b5d60718293a4b5c6d7e8f9012345678abcdef01";

    [Theory]
    [InlineData("v1.3.10", 1, 3, 10)]
    [InlineData(" 1.1.11 ", 1, 1, 11)]
    [InlineData("v1.4.0+build.5", 1, 4, 0)]
    public void TryParseVersion_ReadsPlainReleaseTags(string tag, int major, int minor, int build)
    {
        var parsed = UpdateService.TryParseVersion(tag, out var version);

        Assert.True(parsed);
        Assert.Equal(new Version(major, minor, build), version);
    }

    [Fact]
    public void TryParseVersion_AcceptsTwoPartTags()
    {
        Assert.True(UpdateService.TryParseVersion("V2.0", out var version));
        Assert.Equal(new Version(2, 0), version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("latest")]
    [InlineData("v1.4.0-beta.1")]
    [InlineData("v1.4.0-rc1")]
    [InlineData(null)]
    public void TryParseVersion_RejectsPrereleaseAndInvalidTags(string? tag)
    {
        Assert.False(UpdateService.TryParseVersion(tag, out _));
    }

    [Fact]
    public void IsNewerVersion_ComparesAllComponents()
    {
        Assert.True(UpdateService.IsNewerVersion(new Version(1, 3, 10), new Version(1, 3, 11)));
        Assert.True(UpdateService.IsNewerVersion(new Version(1, 3, 10), new Version(1, 4, 0)));
        Assert.True(UpdateService.IsNewerVersion(new Version(1, 3, 10), new Version(2, 0, 0)));
        Assert.False(UpdateService.IsNewerVersion(new Version(1, 3, 10), new Version(1, 3, 10)));
        Assert.False(UpdateService.IsNewerVersion(new Version(1, 3, 10), new Version(1, 3, 9)));
    }

    [Fact]
    public void IsNewerVersion_TreatsMissingRevisionAsZeroToBlockDowngrades()
    {
        // The assembly version is 1.3.10.0 while the release tag is v1.3.10; these must be equal.
        Assert.False(UpdateService.IsNewerVersion(new Version(1, 3, 10, 0), new Version(1, 3, 10)));
        Assert.False(UpdateService.IsNewerVersion(new Version(1, 3, 10), new Version(1, 3, 10, 0)));
        Assert.True(UpdateService.IsNewerVersion(new Version(1, 3, 10, 0), new Version(1, 3, 10, 1)));
        Assert.True(UpdateService.IsNewerVersion(new Version(1, 3, 10), new Version(1, 3, 11, 0)));
    }

    [Fact]
    public void SelectPackageAsset_PrefersWindowsX64Archive()
    {
        var assets = new[]
        {
            new UpdateAssetInfo("WuGesture-1.3.10-source.zip", "https://example.com/source.zip", 10),
            new UpdateAssetInfo("WuGesture-v1.3.10-symbols.zip", "https://example.com/symbols.zip", 20),
            new UpdateAssetInfo("WuGesture-v1.3.10-windows-x64.zip", "https://example.com/app.zip", 30)
        };

        var selected = UpdateService.SelectPackageAsset(assets);

        Assert.NotNull(selected);
        Assert.Equal("WuGesture-v1.3.10-windows-x64.zip", selected.Name);
        Assert.Equal("https://example.com/app.zip", selected.DownloadUrl);
    }

    [Fact]
    public void SelectPackageAsset_ReturnsNullWithoutMatchingArchive()
    {
        var assets = new[]
        {
            new UpdateAssetInfo("WuGesture-debug-windows-x64.zip", "https://example.com/debug.zip", 10),
            new UpdateAssetInfo("WuGesture-v1.3.10-windows-x64.zip", "", 20),
            new UpdateAssetInfo("WuGesture-v2.0.0-beta.1-windows-x64.zip", "https://example.com/beta.zip", 30)
        };

        Assert.Null(UpdateService.SelectPackageAsset(assets));
    }

    [Theory]
    [InlineData("WuGesture/WuGesture.exe", true)]
    [InlineData("WuGesture/Web/index.html", true)]
    [InlineData("WuGesture\\Web\\index.html", true)]
    [InlineData("../WuGesture.exe", false)]
    [InlineData("WuGesture/../../evil.exe", false)]
    [InlineData("/Windows/System32/evil.exe", false)]
    [InlineData("C:/Windows/system32/evil.exe", false)]
    [InlineData("", false)]
    public void IsSafeArchivePath_RejectsTraversalAndAbsolutePaths(string entry, bool expected)
    {
        Assert.Equal(expected, UpdateService.IsSafeArchivePath(entry));
    }

    [Theory]
    [InlineData("gitee", UpdateSources.Gitee)]
    [InlineData("GITEE", UpdateSources.Gitee)]
    [InlineData("github", UpdateSources.GitHub)]
    [InlineData("GitHub", UpdateSources.GitHub)]
    [InlineData("unknown", UpdateSources.Gitee)]
    [InlineData("", UpdateSources.Gitee)]
    [InlineData(null, UpdateSources.Gitee)]
    public void NormalizeUpdateSource_FallsBackToGitee(string? value, string expected)
    {
        Assert.Equal(expected, UpdateSources.Normalize(value));
    }

    [Fact]
    public void ParseChecksumFromNotes_ReadsArchiveHashFromReleaseNotes()
    {
        const string notes =
            "## 更新内容\r\n\r\n- fix: something\r\n\r\n## 校验\r\n\r\n" +
            "SHA256 (WuGesture-v1.4.0-windows-x64.zip): " + HashA + "\r\n";

        Assert.Equal(HashA, UpdateService.ParseChecksumFromNotes(notes, "WuGesture-v1.4.0-windows-x64.zip"));
        // Release tags and file names are matched without regard to case.
        Assert.Equal(HashA, UpdateService.ParseChecksumFromNotes(notes, "wugesture-v1.4.0-windows-x64.ZIP"));
    }

    [Fact]
    public void ParseChecksumFromNotes_AcceptsUnnamedAndFullWidthLine()
    {
        Assert.Equal(HashA, UpdateService.ParseChecksumFromNotes($"SHA256: {HashA}", "WuGesture-v1.4.0-windows-x64.zip"));
        Assert.Equal(HashA, UpdateService.ParseChecksumFromNotes($"SHA256（WuGesture-v1.4.0-windows-x64.zip）：{HashA}", "WuGesture-v1.4.0-windows-x64.zip"));
    }

    [Fact]
    public void ParseChecksumFromNotes_IgnoresOtherAssetsAndInvalidHashes()
    {
        var notes =
            $"SHA256 (WuGesture-v1.5.0-windows-x64.zip): {HashA}\r\n" +
            "SHA256 (WuGesture-v1.4.0-windows-x64.zip): abcdef\r\n";

        Assert.Null(UpdateService.ParseChecksumFromNotes(notes, "WuGesture-v1.4.0-windows-x64.zip"));
        Assert.Null(UpdateService.ParseChecksumFromNotes("## 更新内容\r\n\r\n- fix: something\r\n", "WuGesture-v1.4.0-windows-x64.zip"));
        Assert.Null(UpdateService.ParseChecksumFromNotes("", "WuGesture-v1.4.0-windows-x64.zip"));
        Assert.Null(UpdateService.ParseChecksumFromNotes(null, "WuGesture-v1.4.0-windows-x64.zip"));
    }

    [Fact]
    public void GetUpdateSource_UsesMirrorRepositories()
    {
        var gitee = UpdateSources.Get(UpdateSources.Gitee);
        var github = UpdateSources.Get(UpdateSources.GitHub);

        Assert.Contains("gitee.com", gitee.ApiBaseUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api.github.com", github.ApiBaseUrl, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(gitee.ApiBaseUrl, github.ApiBaseUrl);
    }
}
