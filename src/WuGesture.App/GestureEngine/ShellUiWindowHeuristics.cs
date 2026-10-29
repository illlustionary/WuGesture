namespace WuGesture.App.GestureEngine;

/// <summary>
/// 决定一个窗口属于“用户应用窗口”还是 Windows Shell 界面（任务视图、Alt+Tab 切换器、
/// 任务栏、开始菜单、搜索、Shell 弹层等）的纯逻辑部分。窗口控制动作只能作用于应用窗口：
/// 向 Shell 内部窗口发送 ShowWindow/PostMessage(WM_CLOSE) 会让 Shell 进入未定义状态，
/// 历史上对任务视图窗口执行“最小化”直接让 explorer.exe 在 Windows.UI.Xaml.dll 中崩溃
/// （0xc000027b），随后整个 Shell 重启、系统卡死。
/// </summary>
internal static class ShellUiWindowHeuristics
{
    // Shell 自身持有的窗口类。命中任意一个都视为 Shell UI，禁止窗口控制。
    // 这些窗口类与承载进程无关，Shell 更新后换了宿主进程也仍然会被拦住。
    private static readonly HashSet<string> ShellWindowClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        // 桌面外壳及其托管视图（Wallpaper Engine 等会创建 WorkerW）。
        "Progman",
        "WorkerW",
        "SHELLDLL_DefView",
        "SysListView32",

        // 任务栏、通知区域和溢出面板。
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "NotifyIconOverflowWindow",
        "TopLevelWindowForOverflowXamlIsland",

        // 窗口切换界面：Win+Tab 任务视图、Alt+Tab 切换器及其缩略图/XAML 宿主。
        "MultitaskingViewFrame",
        "TaskListWnd",
        "TaskListThumbnailWnd",
        "TaskSwitcherWnd",
        "XamlExplorerHostIslandWindow",
        "Windows.UI.Core.CoreWindow",
        "Windows.Internal.Shell.TabProxyWindow",
        "Xaml_WindowedPopupClass",
        "Windows.UI.Composition.DesktopWindowContentBridge",

        // 应用切换/前台激活过渡窗口。
        "ForegroundStaging",
        "ApplicationManager_DesktopShellWindow",
    };

    // 承载 Shell 界面的系统进程。除 explorer.exe 外，这些进程的窗口全部按 Shell UI 处理。
    private static readonly HashSet<string> ShellHostProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer",
        "shellexperiencehost",
        "startmenuexperiencehost",
        "searchui",
        "searchhost",
        "searchapp",
        "textinputhost",
        "sihost",
        "shellhost",
        "lockapp",
    };

    // explorer.exe 同时承载文件夹窗口和 Shell UI。只有下面这些真正属于文件浏览/对话框的
    // 窗口类可以接收窗口控制；其余 explorer 窗口一律按 Shell UI 处理，这样以后新增的
    // Shell XAML 窗口类即使不在上面的清单里也不会被误操作。
    private static readonly HashSet<string> ExplorerApplicationWindowClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "CabinetWClass",
        "ExploreWClass",
        "#32770",
    };

    private const string ExplorerProcessName = "explorer";

    public static bool IsShellUiWindow(string className, string processName)
    {
        if (IsShellWindowClass(className))
        {
            return true;
        }

        if (!IsShellHostProcess(processName))
        {
            return false;
        }

        return !(IsExplorerProcess(processName) && IsExplorerApplicationWindowClass(className));
    }

    public static bool IsShellWindowClass(string className)
    {
        return className.Length > 0 && ShellWindowClasses.Contains(className);
    }

    public static bool IsShellHostProcess(string processName)
    {
        return processName.Length > 0 && ShellHostProcesses.Contains(processName);
    }

    public static bool IsExplorerApplicationWindowClass(string className)
    {
        return className.Length > 0 && ExplorerApplicationWindowClasses.Contains(className);
    }

    /// <summary>
    /// 隐藏窗口、被 DWM 遮蔽的窗口（其他虚拟桌面、挂起的 UWP 应用等）不是可交互的应用窗口，
    /// 也不应该接收窗口控制或前置激活。
    /// </summary>
    public static bool IsInteractableWindow(bool isVisible, bool isCloaked)
    {
        return isVisible && !isCloaked;
    }

    private static bool IsExplorerProcess(string processName)
    {
        return string.Equals(processName, ExplorerProcessName, StringComparison.OrdinalIgnoreCase);
    }
}
