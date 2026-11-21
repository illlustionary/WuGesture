namespace WuGesture.App.GestureEngine;

/// <summary>
/// Serialises configuration writes on a dedicated background thread and collapses a burst of edits into a
/// single write. Preparing and applying the configuration stays on the UI thread, but the atomic file write
/// (which ends with a flush-to-disk) no longer blocks the message loop while the user drags a slider or
/// toggles several switches in a row.
/// </summary>
internal sealed class ConfigWriteScheduler : IDisposable
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(3);

    private readonly GestureConfigStore store;
    private readonly Action<Action> dispatch;
    private readonly Action<bool, string> reportCompleted;
    private readonly object gate = new();
    private readonly Thread worker;
    private GestureConfig? pending;
    private bool isWriting;
    private bool isStopping;
    private bool isDisposed;

    public ConfigWriteScheduler(
        GestureConfigStore store,
        Action<Action> dispatch,
        Action<bool, string> reportCompleted)
    {
        this.store = store;
        this.dispatch = dispatch;
        this.reportCompleted = reportCompleted;
        worker = new Thread(Run)
        {
            IsBackground = true,
            Name = "WuGesture.ConfigWriter"
        };
        worker.Start();
    }

    /// <summary>
    /// Queues a configuration write. A write that has not started yet is replaced instead of queued, so
    /// holding a key down or dragging a slider results in one write with the final value.
    /// </summary>
    public void Enqueue(GestureConfig config)
    {
        lock (gate)
        {
            if (isStopping)
            {
                return;
            }

            pending = config;
            Monitor.Pulse(gate);
        }
    }

    /// <summary>
    /// Blocks until every queued write has finished. Callers that read the file back (reload, import,
    /// WebDAV restore) or replace it directly must call this first, otherwise they race the background
    /// writer and can read a stale file.
    /// </summary>
    public bool Flush(TimeSpan timeout)
    {
        var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        lock (gate)
        {
            while (pending is not null || isWriting)
            {
                var remaining = deadline - Environment.TickCount64;
                if (remaining <= 0)
                {
                    return false;
                }

                Monitor.Wait(gate, (int)Math.Min(remaining, int.MaxValue));
            }
        }

        return true;
    }

    private void Run()
    {
        while (true)
        {
            GestureConfig next;
            lock (gate)
            {
                while (!isStopping && pending is null)
                {
                    Monitor.Wait(gate);
                }

                if (pending is null)
                {
                    // Stopping with nothing left to write.
                    return;
                }

                next = pending;
                pending = null;
                isWriting = true;
            }

            var success = true;
            var error = "";
            try
            {
                store.Save(next);
            }
            catch (Exception exception)
            {
                success = false;
                error = exception.Message;
                AppLogger.Warning("ConfigWriteScheduler", "config-write-failed", error, exception);
            }

            lock (gate)
            {
                isWriting = false;
                Monitor.PulseAll(gate);
            }

            Report(success, error);
        }
    }

    private void Report(bool success, string error)
    {
        try
        {
            dispatch(() => reportCompleted(success, error));
        }
        catch (Exception exception)
        {
            AppLogger.Warning(
                "ConfigWriteScheduler",
                "config-write-report-failed",
                exception.Message,
                exception);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            isStopping = true;
            Monitor.PulseAll(gate);
        }

        // Let the last queued write finish so closing the window cannot drop the most recent edit.
        worker.Join(ShutdownTimeout);
    }
}
