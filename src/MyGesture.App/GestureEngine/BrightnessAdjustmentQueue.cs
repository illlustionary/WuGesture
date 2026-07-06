namespace MyGesture.App.GestureEngine;

internal static class BrightnessAdjustmentQueue
{
    private static readonly object Sync = new();
    private static int pendingDelta;
    private static bool running;
    private static int? cachedBrightness;

    public static void Enqueue(BrightnessControlAction action)
    {
        var amount = Math.Max(1, action.Amount);
        var delta = action.Operation == BrightnessControlOperation.Decrease ? -amount : amount;

        lock (Sync)
        {
            pendingDelta += delta;
            if (running)
            {
                return;
            }

            running = true;
        }

        _ = Task.Run(ProcessPending);
    }

    private static void ProcessPending()
    {
        while (true)
        {
            int delta;
            lock (Sync)
            {
                delta = pendingDelta;
                pendingDelta = 0;
                if (delta == 0)
                {
                    running = false;
                    return;
                }
            }

            try
            {
                var current = cachedBrightness ?? BrightnessController.GetBrightness();
                var next = Math.Max(0, Math.Min(100, current + delta));
                BrightnessController.SetBrightness(next);
                cachedBrightness = next;
                LevelOsdForm.ShowBrightness(next);
            }
            catch
            {
                cachedBrightness = null;
            }
        }
    }
}
