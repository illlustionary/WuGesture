namespace WuGesture.App;

internal static class WebViewHostContract
{
    public const string HostName = "gesture.wu.philosophy";
    public const string EntryPath = "index.html";
    public const string OutputRootFolder = "Web";

    public static Uri EntryUri => new($"https://{HostName}/{EntryPath}");
}
