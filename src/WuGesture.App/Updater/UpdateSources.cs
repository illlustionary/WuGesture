namespace WuGesture.App.Updater;

internal sealed record UpdateSourceDefinition(
    string Id,
    string DisplayName,
    string ApiBaseUrl,
    string ReleasePageUrl);

internal static class UpdateSources
{
    public const string Gitee = "gitee";
    public const string GitHub = "github";

    private static readonly UpdateSourceDefinition GiteeSource = new(
        Gitee,
        "Gitee",
        "https://gitee.com/api/v5/repos/neko_nya/WuGesture",
        "https://gitee.com/neko_nya/WuGesture/releases");

    private static readonly UpdateSourceDefinition GitHubSource = new(
        GitHub,
        "GitHub",
        "https://api.github.com/repos/illlustionary/WuGesture",
        "https://github.com/illlustionary/WuGesture/releases");

    public static string Normalize(string? value)
    {
        return string.Equals(value, GitHub, StringComparison.OrdinalIgnoreCase) ? GitHub : Gitee;
    }

    public static UpdateSourceDefinition Get(string? value)
    {
        return Normalize(value) == GitHub ? GitHubSource : GiteeSource;
    }
}
