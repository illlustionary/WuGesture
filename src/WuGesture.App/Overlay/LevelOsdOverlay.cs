using System.Threading;

namespace WuGesture.App.GestureEngine;

internal enum LevelOsdKind
{
    Volume,
    Brightness
}

internal readonly record struct LevelOsdRequest(
    LevelOsdKind Kind,
    int Value,
    bool Muted,
    bool ForceShow = false);

internal static class LevelOsdOverlay
{
    private static readonly object Sync = new();
    private static SynchronizationContext? synchronizationContext;
    private static Action<LevelOsdRequest>? showHandler;

    public static void Configure(SynchronizationContext context, Action<LevelOsdRequest> handler)
    {
        lock (Sync)
        {
            synchronizationContext = context;
            showHandler = handler;
        }
    }

    public static void Reset()
    {
        lock (Sync)
        {
            synchronizationContext = null;
            showHandler = null;
        }
    }

    public static void ShowVolume(int volume, bool muted)
    {
        Show(new LevelOsdRequest(LevelOsdKind.Volume, volume, muted));
    }

    public static void ShowBrightness(int brightness)
    {
        Show(new LevelOsdRequest(LevelOsdKind.Brightness, brightness, false));
    }

    public static void ShowVolumePreview(int volume)
    {
        Show(new LevelOsdRequest(LevelOsdKind.Volume, volume, false, ForceShow: true));
    }

    public static void ShowBrightnessPreview(int brightness)
    {
        Show(new LevelOsdRequest(LevelOsdKind.Brightness, brightness, false, ForceShow: true));
    }

    private static void Show(LevelOsdRequest request)
    {
        SynchronizationContext? context;
        Action<LevelOsdRequest>? handler;
        lock (Sync)
        {
            context = synchronizationContext;
            handler = showHandler;
        }

        if (context is null || handler is null)
        {
            return;
        }

        context.Post(_ => handler(request), null);
    }
}
