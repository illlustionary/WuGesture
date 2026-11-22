using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

/// <summary>
/// 快捷键动作在前台激活失败后的处理结果。用于日志和降级判定，不参与规则匹配。
/// </summary>
internal enum HotkeyForegroundOutcome
{
    /// <summary>目标不是可识别的应用窗口，保持原有的最佳努力行为。</summary>
    NotApplicable,

    /// <summary>目标窗口已经在运行，无需激活。</summary>
    AlreadyForeground,

    /// <summary>本次调用后目标窗口已成为前台。</summary>
    Activated,

    /// <summary>
    /// 当前前台窗口本身无法被替换：已经被最小化（UWP ApplicationFrameHost 窗口在
    /// SW_MINIMIZE 后会继续占据前台），或者是任务栏、任务视图、前台过渡窗口等 Shell UI。
    /// 这时 SetForegroundWindow 没有成功机会，不再等待 150ms。
    /// </summary>
    ForegroundUnrecoverable,

    /// <summary>目标窗口有激活机会，但等满宽限期仍不是前台。</summary>
    ActivationDenied
}

/// <summary>
/// 快捷键动作的前台激活与降级投递策略。纯逻辑部分放在这里，便于不依赖 Win32 做单元测试。
/// </summary>
internal static class HotkeyDeliveryPolicy
{
    // WM_KEYDOWN/WM_KEYUP 的 lParam：bit 16-23 是扫描码，bit 24 是扩展键。
    private const int ExtendedKeyFlag = 0x01000000;

    // 抬键时同时置位 bit 30（前一次状态为按下）和 bit 31（正在转换）。
    private const uint KeyUpTransitionFlags = 0xC0000000;

    /// <summary>
    /// 只有单个非修饰键才允许直接投递 WM_KEYDOWN/WM_KEYUP。组合键依赖目标窗口真实的键盘
    /// 状态，投递到非前台窗口会产生歧义，因此仍然跳过。
    /// </summary>
    public static bool CanPostToTarget(HotkeyAction action)
    {
        if (action.Keys.Count != 1)
        {
            return false;
        }

        var key = action.Keys[0];
        if (key == Keys.None || (key & Keys.Modifiers) != Keys.None)
        {
            return false;
        }

        return !IsModifierKey(key);
    }

    public static bool IsModifierKey(Keys key)
    {
        return key is
            Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or
            Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or
            Keys.Menu or Keys.LMenu or Keys.RMenu or
            Keys.LWin or Keys.RWin;
    }

    /// <summary>
    /// 前台是已最小化窗口或 Shell UI 时，后台进程无法通过 SetForegroundWindow 接管前台，
    /// 应直接走降级而不是空等宽限期。
    /// </summary>
    public static bool IsForegroundUnrecoverable(bool isForegroundMinimized, bool isForegroundShellUi)
    {
        return isForegroundMinimized || isForegroundShellUi;
    }

    /// <summary>
    /// 构造 WM_KEYDOWN/WM_KEYUP 的 lParam：重复次数 1、扫描码、扩展键位；抬键再补上
    /// 前一次状态和转换状态位。
    /// </summary>
    public static int BuildKeyMessageLParam(ushort scanCode, bool isExtended, bool keyUp)
    {
        var lParam = 1 | (scanCode << 16);
        if (isExtended)
        {
            lParam |= ExtendedKeyFlag;
        }

        if (keyUp)
        {
            lParam |= unchecked((int)KeyUpTransitionFlags);
        }

        return lParam;
    }

    public static string Describe(HotkeyForegroundOutcome outcome)
    {
        return outcome switch
        {
            HotkeyForegroundOutcome.NotApplicable => "target-not-application-window",
            HotkeyForegroundOutcome.AlreadyForeground => "already-foreground",
            HotkeyForegroundOutcome.Activated => "activated",
            HotkeyForegroundOutcome.ForegroundUnrecoverable => "foreground-minimized-or-shell-ui",
            HotkeyForegroundOutcome.ActivationDenied => "activation-denied",
            _ => "unknown"
        };
    }
}
