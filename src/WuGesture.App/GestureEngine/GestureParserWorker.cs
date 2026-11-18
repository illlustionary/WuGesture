namespace WuGesture.App.GestureEngine;

internal sealed class GestureParserWorker : IDisposable
{
    private readonly object queueLock = new();
    private readonly Queue<Action> queue = [];
    private Action? latestMove;
    private Thread? thread;
    private bool stopRequested;
    private bool disposed;

    public void Start()
    {
        lock (queueLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (thread is not null)
            {
                return;
            }

            stopRequested = false;
            EnsureWorkerLocked();
        }
    }

    public void Enqueue(Action action)
    {
        lock (queueLock)
        {
            if (stopRequested || disposed)
            {
                return;
            }

            EnsureWorkerLocked();
            queue.Enqueue(action);
            Monitor.Pulse(queueLock);
        }
    }

    public void EnqueueLatestMove(Action action)
    {
        lock (queueLock)
        {
            if (stopRequested || disposed)
            {
                return;
            }

            EnsureWorkerLocked();
            latestMove = action;
            Monitor.Pulse(queueLock);
        }
    }

    public void EnqueueAfterLatestMove(Action action)
    {
        lock (queueLock)
        {
            if (stopRequested || disposed)
            {
                return;
            }

            EnsureWorkerLocked();
            if (latestMove is not null)
            {
                queue.Enqueue(latestMove);
                latestMove = null;
            }

            queue.Enqueue(action);
            Monitor.Pulse(queueLock);
        }
    }

    private void EnsureWorkerLocked()
    {
        if (thread is not null)
        {
            return;
        }

        thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest,
            Name = "WuGesture Parser"
        };
        thread.Start();
    }

    public void Dispose()
    {
        Thread? workerThread;
        lock (queueLock)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            stopRequested = true;
            queue.Clear();
            latestMove = null;
            workerThread = thread;
            Monitor.PulseAll(queueLock);
        }

        if (workerThread is not null && workerThread != Thread.CurrentThread)
        {
            workerThread.Join(TimeSpan.FromSeconds(3));
        }

        GC.SuppressFinalize(this);
    }

    private void ThreadMain()
    {
        try
        {
            while (TakeNext() is { } action)
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    // A single bad event (config reload race, malformed rule, unexpected input) must not
                    // stop the whole gesture pipeline for the rest of the process lifetime.
                    if (!disposed)
                    {
                        AppLogger.Error(
                            "GestureParserWorker",
                            "action-failed",
                            "A queued gesture parser action failed; the parser keeps running.",
                            exception);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            if (!disposed)
            {
                AppLogger.Error(
                    "GestureParserWorker",
                    "thread-failed",
                    "The dedicated gesture parser stopped unexpectedly and will be restarted on demand.",
                    exception);
            }
        }
        finally
        {
            lock (queueLock)
            {
                if (ReferenceEquals(thread, Thread.CurrentThread))
                {
                    thread = null;
                }

                if (disposed || stopRequested)
                {
                    queue.Clear();
                    latestMove = null;
                }
                else if (queue.Count > 0 || latestMove is not null)
                {
                    // Work arrived while the worker was stopping; keep the pipeline alive.
                    EnsureWorkerLocked();
                }
            }
        }
    }

    private Action? TakeNext()
    {
        lock (queueLock)
        {
            while (!stopRequested && queue.Count == 0 && latestMove is null)
            {
                Monitor.Wait(queueLock);
            }

            if (stopRequested)
            {
                return null;
            }

            if (queue.Count > 0)
            {
                return queue.Dequeue();
            }

            var move = latestMove;
            latestMove = null;
            return move;
        }
    }
}
