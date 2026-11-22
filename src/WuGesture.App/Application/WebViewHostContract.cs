namespace WuGesture.App;

internal static class WebViewHostContract
{
    /// <summary>
    /// 配置界面的虚拟主机名。必须落在保证不可注册的命名空间里：WebView2 用这个主机名映射本地
    /// 文件夹，同时它也是入站消息的可信来源判定条件，所以不能让第三方有机会在该 origin 提供内容。
    /// `.invalid` 是 RFC 6761 保留的顶级域，永远不会被委派；即使映射意外缺失，导航也只会失败，
    /// 不会落到别人注册的站点上。
    /// </summary>
    public const string HostName = "appassets.wugesture.invalid";
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
