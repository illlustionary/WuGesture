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
            thread = new Thread(ThreadMain)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = "WuGesture Parser"
            };
            thread.Start();
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

            if (latestMove is not null)
            {
                queue.Enqueue(latestMove);
                latestMove = null;
            }

            queue.Enqueue(action);
            Monitor.Pulse(queueLock);
        }
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
                action();
            }
        }
        catch (Exception exception)
        {
            if (!disposed)
            {
                AppLogger.Error(
                    "GestureParserWorker",
                    "thread-failed",
                    "The dedicated gesture parser stopped unexpectedly.",
                    exception);
            }
        }
        finally
        {
            lock (queueLock)
            {
                thread = null;
                stopRequested = true;
                queue.Clear();
                latestMove = null;
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
