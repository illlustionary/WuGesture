namespace WuGesture.App;

static class Program
{
    private const string WebView2RuntimeUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";
    private const uint AsfwAny = 0xFFFFFFFF;

    [STAThread]
    static void Main(string[] args)
    {
        AppLogger.Initialize();
        RegisterUnhandledExceptionLogging();
        var isElevatedRelaunch = args.Any(arg =>
            string.Equals(arg, AppIdentity.ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase));
        var isStartupLaunch = args.Any(arg =>
            string.Equals(arg, AppIdentity.StartupLaunchArgument, StringComparison.OrdinalIgnoreCase));
        using var mutex = new Mutex(initiallyOwned: false, AppIdentity.SingleInstanceMutexName);
        var ownsMutex = mutex.WaitOne(isElevatedRelaunch ? TimeSpan.FromSeconds(15) : TimeSpan.Zero);
        if (!ownsMutex)
        {
            AppLogger.Information("Program", "existing-instance-detected", "Forwarding the launch request to the existing instance.");
            SignalExistingInstance();
            AppLogger.Shutdown(TimeSpan.FromSeconds(1));
            return;
        }

        try
        {
            AppLogger.Information("Program", "started", $"Startup launch: {isStartupLaunch}; elevated relaunch: {isElevatedRelaunch}.");
            ApplicationConfiguration.Initialize();
            if (!EnsureWebView2RuntimeAvailable())
            {
                return;
            }

            using var showExistingInstanceEvent = new EventWaitHandle(
                initialState: false,
                mode: EventResetMode.AutoReset,
                name: AppIdentity.ShowExistingInstanceEventName);
            using var showExistingInstanceCompletedEvent = new EventWaitHandle(
                initialState: false,
                mode: EventResetMode.AutoReset,
                name: AppIdentity.ShowExistingInstanceCompletedEventName);
            using var form = new MainForm(isStartupLaunch);
            using var listenerCancellation = new CancellationTokenSource();
            var listener = Task.Run(() => ListenForExistingInstanceRequests(
                showExistingInstanceEvent,
                showExistingInstanceCompletedEvent,
                form,
                listenerCancellation.Token));

            Application.Run(form);

            listenerCancellation.Cancel();
            showExistingInstanceEvent.Set();
            try
            {
                listener.Wait(TimeSpan.FromSeconds(1));
            }
            catch (Exception exception)
            {
                AppLogger.Warning("Program", "instance-listener-stop-failed", "The single-instance listener did not stop within the expected time.", exception);
            }
        }
        finally
        {
            AppLogger.Information("Program", "stopped", "Application process is stopping.");
            AppLogger.Shutdown(TimeSpan.FromSeconds(2));
            mutex.ReleaseMutex();
        }
    }

    private static void RegisterUnhandledExceptionLogging()
    {
        Application.ThreadException += (_, eventArgs) =>
            AppLogger.Error("Program", "ui-thread-unhandled-exception", "An unhandled exception reached the WinForms UI thread.", eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                AppLogger.Error("Program", "unhandled-exception", $"Unhandled exception. Terminating: {eventArgs.IsTerminating}.", exception);
            }
            else
            {
                AppLogger.Error("Program", "unhandled-non-exception", $"Unhandled non-exception object. Terminating: {eventArgs.IsTerminating}.", new InvalidOperationException(eventArgs.ExceptionObject?.ToString() ?? "null"));
            }

            AppLogger.Shutdown(TimeSpan.FromSeconds(1));
        };
        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            AppLogger.Error("Program", "unobserved-task-exception", "An unobserved task exception was raised.", eventArgs.Exception);
            eventArgs.SetObserved();
        };
    }

    private static bool EnsureWebView2RuntimeAvailable()
    {
        try
        {
            _ = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString(null);
            return true;
        }
        catch (Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException exception)
        {
            AppLogger.Warning("Program", "webview2-runtime-missing", "The WebView2 Runtime was not found.", exception);
        }
        catch (DllNotFoundException exception)
        {
            AppLogger.Warning("Program", "webview2-loader-missing", "The WebView2 loader could not be found.", exception);
        }
        catch (BadImageFormatException exception)
        {
            AppLogger.Warning("Program", "webview2-loader-invalid", "The WebView2 loader has an invalid architecture or format.", exception);
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
                using var showExistingInstanceCompletedEvent = EventWaitHandle.OpenExisting(
                    AppIdentity.ShowExistingInstanceCompletedEventName);
                showExistingInstanceCompletedEvent.Reset();
                AllowSetForegroundWindow(AsfwAny);
                showExistingInstanceEvent.Set();
                showExistingInstanceCompletedEvent.WaitOne(TimeSpan.FromSeconds(3));
                return;
            }
            catch (Exception exception)
            {
                AppLogger.Warning("Program", "existing-instance-signal-retry", $"Unable to signal the existing instance on attempt {attempt + 1}.", exception);
                Thread.Sleep(100);
            }
        }

        AppLogger.Warning("Program", "existing-instance-signal-failed", "Unable to signal an existing instance after all retries.");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(uint dwProcessId);

    private static void ListenForExistingInstanceRequests(
        EventWaitHandle showExistingInstanceEvent,
        EventWaitHandle showExistingInstanceCompletedEvent,
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
                form.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        form.ShowExistingInstance();
                    }
                    finally
                    {
                        showExistingInstanceCompletedEvent.Set();
                    }
                }));
            }
            catch (Exception exception)
            {
                AppLogger.Warning("Program", "existing-instance-ui-dispatch-failed", "Unable to dispatch the existing-instance request to the UI thread.", exception);
            }
        }
    }
}
