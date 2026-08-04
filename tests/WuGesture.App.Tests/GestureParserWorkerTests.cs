using System.Collections.Concurrent;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureParserWorkerTests
{
    [Fact]
    public void EnqueueAfterLatestMove_PreservesTheLastMoveBeforeTheEndEvent()
    {
        var completed = new CountdownEvent(3);
        var executionOrder = new ConcurrentQueue<string>();
        using var worker = new GestureParserWorker();

        void Record(string name)
        {
            executionOrder.Enqueue(name);
            completed.Signal();
        }

        worker.Enqueue(() => Record("start"));
        worker.EnqueueLatestMove(() => Record("move"));
        worker.EnqueueAfterLatestMove(() => Record("end"));
        worker.Start();

        Assert.True(completed.Wait(TimeSpan.FromSeconds(2)));
        Assert.Equal(["start", "move", "end"], executionOrder);
    }
}
