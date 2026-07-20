namespace WuGesture.App;

static class Program
{
    private const string WebView2RuntimeUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";

    [STAThread]
    static void Main(string[] args)
    {
        var isElevatedRelaunch = args.Any(arg =>
            string.Equals(arg, AppIdentity.ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase));
        var isStartupLaunch = args.Any(arg =>
            string.Equals(arg, AppIdentity.StartupLaunchArgument, StringComparison.OrdinalIgnoreCase));
        using var mutex = new Mutex(initiallyOwned: false, AppIdentity.SingleInstanceMutexName);
        var ownsMutex = mutex.WaitOne(isElevatedRelaunch ? TimeSpan.FromSeconds(15) : TimeSpan.Zero);
        if (!ownsMutex)
        {
            SignalExistingInstance();
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            if (!EnsureWebView2RuntimeAvailable())
            {
                return;
            }

            using var showExistingInstanceEvent = new EventWaitHandle(
                initialState: false,
                mode: EventResetMode.AutoReset,
                name: AppIdentity.ShowExistingInstanceEventName);
            using var form = new MainForm(isStartupLaunch);
            using var listenerCancellation = new CancellationTokenSource();
            var listener = Task.Run(() => ListenForExistingInstanceRequests(
                showExistingInstanceEvent,
                form,
                listenerCancellation.Token));

            Application.Run(form);

            listenerCancellation.Cancel();
            showExistingInstanceEvent.Set();
            try
            {
                listener.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
            }
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static bool EnsureWebView2RuntimeAvailable()
    {
        try
        {
            _ = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString(null);
            return true;
        }
        catch (Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException)
        {
        }
        catch (DllNotFoundException)
        {
        }
        catch (BadImageFormatException)
        {
        }

        var result = MessageBox.Show(
            "WuGesture 需要 Microsoft Edge WebView2 Runtime 才能显示配置界面。\n\n是否打开微软官方下载页面进行安装？",
            "WuGesture - 缺少运行环境",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Error);
        if (result == DialogResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = WebView2RuntimeUrl,
                UseShellExecute = true
            });
        }

        return false;
    }

    private static void SignalExistingInstance()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var showExistingInstanceEvent = EventWaitHandle.OpenExisting(AppIdentity.ShowExistingInstanceEventName);
                showExistingInstanceEvent.Set();
                return;
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    private static void ListenForExistingInstanceRequests(
        EventWaitHandle showExistingInstanceEvent,
        MainForm form,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!showExistingInstanceEvent.WaitOne(TimeSpan.FromMilliseconds(250)))
            {
                continue;
            }

            var waitUntil = DateTime.UtcNow.AddSeconds(3);
            while (!cancellationToken.IsCancellationRequested &&
                !form.IsDisposed &&
                !form.IsHandleCreated &&
                DateTime.UtcNow < waitUntil)
            {
                Thread.Sleep(50);
            }

            if (cancellationToken.IsCancellationRequested || form.IsDisposed || !form.IsHandleCreated)
            {
                continue;
            }

            try
            {
                form.BeginInvoke(new Action(form.ShowExistingInstance));
            }
            catch
            {
            }
        }
    }
}
