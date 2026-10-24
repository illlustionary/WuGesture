namespace WuGesture.App;

internal static class WebViewHostContract
{
    public const string HostName = "gesture.wu.philosophy";
    public const string EntryPath = "index.html";
    public const string OutputRootFolder = "Web";
    public const string UserDataFolderName = "WebView2";

    public static Uri EntryUri => new($"https://{HostName}/{EntryPath}");

    /// <summary>
    /// The WebView2 profile lives under the user's local application data instead of next to the executable:
    /// the install directory may be read-only (Program Files) and publish scripts clear the output folder.
    /// </summary>
    public static string ResolveUserDataFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppIdentity.AppDataFolderName,
            UserDataFolderName);
    }
}
