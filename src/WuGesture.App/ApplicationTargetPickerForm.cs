using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WuGesture.App;

public sealed class ApplicationTargetPickerForm : Form
{
    private const int WsExToolWindow = 0x00000080;
    private const int WsExTransparent = 0x00000020;
    private const int WmNcHitTest = 0x0084;
    private const int HtTransparent = -1;

    private readonly Cursor pickerCursor = Cursors.Cross;
    private readonly Bitmap pickerImage;
    private readonly int currentProcessId = Environment.ProcessId;
    private LowLevelMouseProc? mouseProc;
    private IntPtr mouseHook;
    private bool cursorHidden;
    private bool completed;

    public PickedApplication? PickedApplication { get; private set; }
    public string ErrorMessage { get; private set; } = "";

    public ApplicationTargetPickerForm(Color cursorColor)
    {
        pickerImage = CreatePickerImage(cursorColor);
        ClientSize = pickerCursor.Size;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = Color.Fuchsia;
        TransparencyKey = Color.Fuchsia;

        Paint += OnPaint;
        Shown += OnShown;
        FormClosed += (_, _) =>
        {
            StopPicking();
            pickerImage.Dispose();
        };
        KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Escape)
            {
                Complete(DialogResult.Cancel);
            }
        };
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExToolWindow | WsExTransparent;
            return createParams;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmNcHitTest)
        {
            m.Result = new IntPtr(HtTransparent);
            return;
        }

        base.WndProc(ref m);
    }

    private void OnShown(object? sender, EventArgs e)
    {
        try
        {
            StartPicking();
            UpdatePickerCursor(Cursor.Position);
        }
        catch
        {
            StopPicking();
            throw;
        }
    }

    private void StartPicking()
    {
        mouseProc = MouseHookCallback;
        mouseHook = SetWindowsHookEx(WhMouseLl, mouseProc, GetModuleHandle(null), 0);
        if (mouseHook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "未能开始窗口拾取。");
        }

        Cursor.Hide();
        cursorHidden = true;
    }

    private void StopPicking()
    {
        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }

        if (cursorHidden)
        {
            Cursor.Show();
            cursorHidden = false;
            SetCursor(Cursors.Default.Handle);
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            if (wParam == WmMouseMove)
            {
                var mouseData = Marshal.PtrToStructure<MouseHookData>(lParam);
                UpdatePickerCursor(mouseData.Point);
            }

            if (wParam == WmLButtonUp)
            {
                PickAtCursor();
                return new IntPtr(1);
            }
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

    private void UpdatePickerCursor(Point cursorPosition)
    {
        var hotspot = pickerCursor.HotSpot;
        Location = new Point(cursorPosition.X - hotspot.X, cursorPosition.Y - hotspot.Y);
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(pickerImage, Point.Empty);
    }

    private Bitmap CreatePickerImage(Color cursorColor)
    {
        var image = new Bitmap(pickerCursor.Size.Width, pickerCursor.Size.Height);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Fuchsia);
        pickerCursor.Draw(graphics, new Rectangle(Point.Empty, pickerCursor.Size));

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                if (image.GetPixel(x, y).ToArgb() != Color.Fuchsia.ToArgb())
                {
                    image.SetPixel(x, y, cursorColor);
                }
            }
        }

        return image;
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
    private static readonly IntPtr WmMouseMove = new(0x0200);
    private static readonly IntPtr WmLButtonUp = new(0x0202);
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint GetAncestorRoot = 2;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseHookData
    {
        public Point Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr hCursor);

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
