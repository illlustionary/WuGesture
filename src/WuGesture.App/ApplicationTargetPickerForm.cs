using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WuGesture.App;

public sealed class ApplicationTargetPickerForm : Form
{
    private readonly System.Windows.Forms.Timer followCursorTimer = new() { Interval = 15 };
    private readonly int currentProcessId = Environment.ProcessId;
    private LowLevelMouseProc? mouseProc;
    private IntPtr mouseHook;
    private bool completed;

    public PickedApplication? PickedApplication { get; private set; }
    public string ErrorMessage { get; private set; } = "";

    public ApplicationTargetPickerForm()
    {
        Width = 52;
        Height = 52;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = Color.White;
        TransparencyKey = Color.White;
        Cursor = Cursors.Cross;

        Paint += OnPaint;
        Shown += OnShown;
        FormClosed += (_, _) => StopPicking();
        followCursorTimer.Tick += (_, _) => FollowCursor();
        KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Escape)
            {
                Complete(DialogResult.Cancel);
            }
        };
    }

    private void OnShown(object? sender, EventArgs e)
    {
        StartPicking();
        FollowCursor();
    }

    private void StartPicking()
    {
        mouseProc = MouseHookCallback;
        mouseHook = SetWindowsHookEx(WhMouseLl, mouseProc, GetModuleHandle(null), 0);
        if (mouseHook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "未能开始窗口拾取。");
        }

        followCursorTimer.Start();
    }

    private void StopPicking()
    {
        followCursorTimer.Stop();
        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WmLButtonUp)
        {
            PickAtCursor();
            return new IntPtr(1);
        }

        return CallNextHookEx(mouseHook, nCode, wParam, lParam);
    }

    private void PickAtCursor()
    {
        try
        {
            var cursorPosition = Cursor.Position;
            Hide();

            var windowHandle = WindowFromPoint(cursorPosition);
            var rootWindowHandle = GetAncestor(windowHandle, GetAncestorRoot);
            if (rootWindowHandle == IntPtr.Zero)
            {
                Complete(DialogResult.Cancel);
                return;
            }

            PickedApplication = ResolveApplication(rootWindowHandle);
            Complete(DialogResult.OK);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            Complete(DialogResult.Abort);
        }
    }

    private void Complete(DialogResult result)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        DialogResult = result;
        Close();
    }

    private void FollowCursor()
    {
        var cursorPosition = Cursor.Position;
        Location = new Point(cursorPosition.X - Width / 2, cursorPosition.Y - Height / 2);
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(30, 30, 30), 2);
        var center = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
        e.Graphics.DrawEllipse(pen, center.X - 14, center.Y - 14, 28, 28);
        e.Graphics.DrawLine(pen, center.X, 4, center.X, ClientSize.Height - 4);
        e.Graphics.DrawLine(pen, 4, center.Y, ClientSize.Width - 4, center.Y);
    }

    private PickedApplication ResolveApplication(IntPtr windowHandle)
    {
        GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId <= 0 || processId == currentProcessId)
        {
            throw new InvalidOperationException("未能读取目标窗口进程。");
        }

        var processName = "";
        try
        {
            using var process = Process.GetProcessById(processId);
            processName = process.ProcessName;
        }
        catch
        {
            // Keep processName empty and fall back to the executable file name below.
        }

        var executablePath = QueryProcessImagePath(processId);
        if (processName.Length == 0)
        {
            processName = Path.GetFileNameWithoutExtension(executablePath);
        }

        return new PickedApplication(processName, executablePath);
    }

    private static string QueryProcessImagePath(int processId)
    {
        var processHandle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "未能打开目标进程。");
        }

        try
        {
            var builder = new StringBuilder(32768);
            var size = builder.Capacity;
            if (!QueryFullProcessImageName(processHandle, 0, builder, ref size))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "未能读取目标程序路径。");
            }

            return builder.ToString();
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    private const int WhMouseLl = 14;
    private static readonly IntPtr WmLButtonUp = new(0x0202);
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint GetAncestorRoot = 2;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}

public sealed record PickedApplication(string Name, string Path);
