namespace MyGesture.App;

internal static class WebViewHostContract
{
    public const string HostName = "appassets.local";
    public const string EntryPath = "index.html";
    public const string OutputRootFolder = "Web";
    public const string OutputDistFolder = "dist";

    public static Uri EntryUri => new($"https://{HostName}/{EntryPath}");
}
