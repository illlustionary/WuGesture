using System.Runtime.InteropServices;
using System.Text;

namespace WuGesture.Bootstrapper;

internal static class Program
{
    private const uint MessageBoxYesNo = 0x00000004;
    private const uint MessageBoxIconError = 0x00000010;
    private const int IdYes = 6;

    private const string DotnetDesktopRuntimeUrl = "https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0/runtime";
    private const string WebView2RuntimeUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";
    private const string DotnetRootOverrideEnvironmentVariable = "WUGESTURE_DOTNET_ROOT_OVERRIDE";

    [STAThread]
    private static int Main()
    {
        var dotnetDesktopRuntimeAvailable = IsDotnetDesktopRuntimeAvailable();
        var webView2RuntimeAvailable = IsEvergreenWebView2RuntimeAvailable();

        if (!dotnetDesktopRuntimeAvailable || !webView2RuntimeAvailable)
        {
            ShowMissingDependencyPrompt(dotnetDesktopRuntimeAvailable, webView2RuntimeAvailable);
            return 1;
        }

        var launcherPath = Environment.ProcessPath;
        var launcherDirectory = launcherPath is null ? null : Path.GetDirectoryName(launcherPath);
        var applicationPath = launcherDirectory is null ? null : Path.Combine(launcherDirectory, "WuGesture.App.exe");

        if (applicationPath is null || !File.Exists(applicationPath))
        {
            MessageBoxW(
                IntPtr.Zero,
                "找不到 WuGesture.App.exe。请完整解压发行包后，再从 WuGesture.exe 启动。",
                "WuGesture - 启动失败",
                MessageBoxIconError);
            return 1;
        }

        return StartApplication(applicationPath) ? 0 : 1;
    }

    private static bool IsDotnetDesktopRuntimeAvailable()
    {
        var dotnetRoot = Environment.GetEnvironmentVariable(DotnetRootOverrideEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(dotnetRoot))
        {
            dotnetRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet");
        }

        var runtimeDirectory = Path.Combine(
            dotnetRoot,
            "shared",
            "Microsoft.WindowsDesktop.App");

        if (!Directory.Exists(runtimeDirectory))
        {
            return false;
        }

        try
        {
            return Directory.EnumerateDirectories(runtimeDirectory)
                .Select(Path.GetFileName)
                .Any(version => version is not null && IsDotnet10Version(version));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsDotnet10Version(string version)
    {
        var separatorIndex = version.IndexOf('.');
        var majorVersionText = separatorIndex >= 0 ? version[..separatorIndex] : version;
        return int.TryParse(majorVersionText, out var majorVersion) && majorVersion == 10;
    }

    private static bool IsEvergreenWebView2RuntimeAvailable()
    {
        IntPtr version = IntPtr.Zero;

        try
        {
            return GetAvailableCoreWebView2BrowserVersionString(null, out version) >= 0 && version != IntPtr.Zero;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        finally
        {
            if (version != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(version);
            }
        }
    }

    private static void ShowMissingDependencyPrompt(bool dotnetDesktopRuntimeAvailable, bool webView2RuntimeAvailable)
    {
        string message;
        if (!dotnetDesktopRuntimeAvailable && !webView2RuntimeAvailable)
        {
            message = "WuGesture 需要以下运行环境才能启动：\n\n- .NET 10 Desktop Runtime (x64)\n- Microsoft Edge WebView2 Runtime\n\n是否打开微软官方下载页面进行安装？";
        }
        else if (!dotnetDesktopRuntimeAvailable)
        {
            message = "WuGesture 需要 .NET 10 Desktop Runtime (x64) 才能启动。\n\n是否打开微软官方下载页面进行安装？";
        }
        else
        {
            message = "WuGesture 需要 Microsoft Edge WebView2 Runtime 才能显示配置界面。\n\n是否打开微软官方下载页面进行安装？";
        }

        if (MessageBoxW(IntPtr.Zero, message, "WuGesture - 缺少运行环境", MessageBoxYesNo | MessageBoxIconError) != IdYes)
        {
            return;
        }

        if (!dotnetDesktopRuntimeAvailable)
        {
            ShellExecuteW(IntPtr.Zero, "open", DotnetDesktopRuntimeUrl, null, null, 1);
        }

        if (!webView2RuntimeAvailable)
        {
            ShellExecuteW(IntPtr.Zero, "open", WebView2RuntimeUrl, null, null, 1);
        }
    }

    private static bool StartApplication(string applicationPath)
    {
        var startupInfo = new StartupInfo
        {
            cb = (uint)Marshal.SizeOf<StartupInfo>()
        };
        var commandLine = new StringBuilder(BuildCommandLine(applicationPath));

        if (CreateProcessW(applicationPath, commandLine, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref startupInfo, out var processInformation))
        {
            CloseHandle(processInformation.hThread);
            CloseHandle(processInformation.hProcess);
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        MessageBoxW(
            IntPtr.Zero,
            $"无法启动 WuGesture.App.exe。Windows 错误代码：{error}。",
            "WuGesture - 启动失败",
            MessageBoxIconError);
        return false;
    }

    private static string BuildCommandLine(string applicationPath)
    {
        var commandLine = new StringBuilder(QuoteArgument(applicationPath));
        foreach (var argument in Environment.GetCommandLineArgs().Skip(1))
        {
            commandLine.Append(' ');
            commandLine.Append(QuoteArgument(argument));
        }

        return commandLine.ToString();
    }

    private static string QuoteArgument(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"']) < 0)
        {
            return argument;
        }

        var quotedArgument = new StringBuilder(argument.Length + 2);
        quotedArgument.Append('"');
        var backslashCount = 0;

        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                quotedArgument.Append('\\', backslashCount * 2 + 1);
                quotedArgument.Append(character);
                backslashCount = 0;
                continue;
            }

            quotedArgument.Append('\\', backslashCount);
            quotedArgument.Append(character);
            backslashCount = 0;
        }

        quotedArgument.Append('\\', backslashCount * 2);
        quotedArgument.Append('"');
        return quotedArgument.ToString();
    }

    [DllImport("WebView2Loader.dll", CharSet = CharSet.Unicode)]
    private static extern int GetAvailableCoreWebView2BrowserVersionString(string? browserExecutableFolder, out IntPtr versionInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ShellExecuteW(IntPtr hwnd, string operation, string file, string? parameters, string? directory, int showCommand);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hwnd, string text, string caption, uint type);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessW(string applicationName, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string? currentDirectory, ref StartupInfo startupInfo, out ProcessInformation processInformation);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public uint cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }
}
