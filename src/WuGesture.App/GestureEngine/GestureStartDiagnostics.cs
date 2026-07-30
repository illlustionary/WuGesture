using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using WuGesture.App;

namespace WuGesture.App.GestureEngine;

internal static class GestureStartDiagnostics
{
    private const long MaximumLogSizeBytes = 1024 * 1024;
    private const string LogFileName = "gesture-start-latency.log";
    private const string FallbackErrorLogFileName = "WuGesture-gesture-diagnostics-error.log";
    private static readonly ConcurrentQueue<GestureDiagnosticSample> pendingSamples = new();
    private static readonly string logDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppIdentity.AppDataFolderName,
        "diagnostics");
    private static int flushScheduled;

    public static void ReportServiceStarted()
    {
        Enqueue(new GestureDiagnosticSample(
            DateTimeOffset.Now,
            "service-start",
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            "",
            IntPtr.Zero,
            "",
            false,
            false,
            false,
            $"pid={Environment.ProcessId}; log={Path.Combine(logDirectory, LogFileName)}"));
    }

    public static void ReportButtonDown(
        long startedAt,
        long contextCompletedAt,
        long exclusionCompletedAt,
        long fullscreenCompletedAt,
        GestureMouseButton button,
        string targetWindowMode,
        IntPtr targetWindow,
        GestureScopeContext scopeContext,
        bool isExcluded,
        bool isFullscreen,
        bool fullscreenChecked)
    {
        Enqueue(new GestureDiagnosticSample(
            DateTimeOffset.Now,
            "button-down",
            ElapsedMilliseconds(startedAt, fullscreenCompletedAt),
            ElapsedMilliseconds(startedAt, contextCompletedAt),
            ElapsedMilliseconds(contextCompletedAt, exclusionCompletedAt),
            ElapsedMilliseconds(exclusionCompletedAt, fullscreenCompletedAt),
            0,
            0,
            0,
            0,
            button,
            targetWindowMode,
            targetWindow,
            scopeContext.AppName,
            isExcluded,
            isFullscreen,
            fullscreenChecked,
            ""));
    }

    public static void ReportButtonDownRecoveredWhileCompleting(
        GestureMouseButton button,
        long sessionId)
    {
        Enqueue(new GestureDiagnosticSample(
            DateTimeOffset.Now,
            "button-down-recovered",
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            button,
            "",
            IntPtr.Zero,
            "",
            false,
            false,
            false,
            $"reason=completing; session={sessionId}"));
    }

    public static void ReportMatchedCompletion(
        long startedAt,
        long postedAt,
        long callbackStartedAt,
        long feedbackCompletedAt,
        long targetCheckedAt,
        long foregroundActivatedAt,
        long actionCompletedAt,
        GestureMouseButton button,
        string actionName,
        string outcome)
    {
        Enqueue(new GestureDiagnosticSample(
            DateTimeOffset.Now,
            "button-up",
            ElapsedMilliseconds(startedAt, actionCompletedAt),
            0,
            0,
            0,
            ElapsedMilliseconds(startedAt, postedAt),
            ElapsedMilliseconds(postedAt, callbackStartedAt),
            ElapsedMilliseconds(callbackStartedAt, feedbackCompletedAt),
            ElapsedMilliseconds(feedbackCompletedAt, actionCompletedAt),
            button,
            "",
            IntPtr.Zero,
            "",
            false,
            false,
            false,
            $"action={actionName}; outcome={outcome}; targetCheck={ElapsedMilliseconds(feedbackCompletedAt, targetCheckedAt):F1}ms; foregroundActivation={ElapsedMilliseconds(targetCheckedAt, foregroundActivatedAt):F1}ms; execution={ElapsedMilliseconds(foregroundActivatedAt, actionCompletedAt):F1}ms"));
    }

    public static void ReportFinalOverlay(long sessionId, OverlayRenderTiming timing)
    {
        Enqueue(new GestureDiagnosticSample(
            DateTimeOffset.Now,
            "overlay-final",
            timing.DrawMilliseconds + timing.PresentMilliseconds,
            0,
            0,
            0,
            0,
            0,
            timing.DrawMilliseconds,
            timing.PresentMilliseconds,
            null,
            "",
            IntPtr.Zero,
            "",
            false,
            false,
            false,
            $"session={sessionId}; dirty={timing.DirtyRect.X},{timing.DirtyRect.Y},{timing.DirtyRect.Width},{timing.DirtyRect.Height}"));
    }

    public static void ReportFirstTrailFrame(
        long sessionId,
        long moveReceivedAt,
        long callbackStartedAt,
        TrailFrameTiming timing)
    {
        var frameCompletedAt = Stopwatch.GetTimestamp();
        Enqueue(new GestureDiagnosticSample(
            DateTimeOffset.Now,
            "trail-first-frame",
            ElapsedMilliseconds(moveReceivedAt, frameCompletedAt),
            0,
            0,
            0,
            0,
            ElapsedMilliseconds(moveReceivedAt, callbackStartedAt),
            timing.PrepareMilliseconds + timing.DrawMilliseconds,
            timing.PresentMilliseconds,
            null,
            "",
            IntPtr.Zero,
            "",
            false,
            false,
            false,
            $"session={sessionId}; prepare={timing.PrepareMilliseconds:F1}ms; draw={timing.DrawMilliseconds:F1}ms"));
    }

    public static void ReportActionExecution(
        long sessionId,
        long startedAt,
        long completedAt,
        string actionName,
        string outcome)
    {
        var executionMilliseconds = ElapsedMilliseconds(startedAt, completedAt);
        Enqueue(new GestureDiagnosticSample(
            DateTimeOffset.Now,
            "action-execution",
            executionMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            executionMilliseconds,
            null,
            "",
            IntPtr.Zero,
            "",
            false,
            false,
            false,
            $"session={sessionId}; action={actionName}; outcome={outcome}"));
    }

    private static double ElapsedMilliseconds(long startTimestamp, long endTimestamp) =>
        Stopwatch.GetElapsedTime(startTimestamp, endTimestamp).TotalMilliseconds;

    private static void Enqueue(GestureDiagnosticSample sample)
    {
        pendingSamples.Enqueue(sample);
        ScheduleFlush();
    }

    private static void ScheduleFlush()
    {
        if (Interlocked.Exchange(ref flushScheduled, 1) == 0)
        {
            ThreadPool.UnsafeQueueUserWorkItem<object?>(static _ => Flush(), null, preferLocal: false);
        }
    }

    private static void Flush()
    {
        try
        {
            var samples = new List<GestureDiagnosticSample>();
            while (pendingSamples.TryDequeue(out var sample))
            {
                samples.Add(sample);
            }

            if (samples.Count == 0)
            {
                return;
            }

            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, LogFileName);
            if (File.Exists(logPath) && new FileInfo(logPath).Length >= MaximumLogSizeBytes)
            {
                File.Move(logPath, logPath + ".previous", overwrite: true);
            }

            File.AppendAllLines(logPath, samples.Select(FormatSample));
        }
        catch (Exception exception)
        {
            WriteFallbackError(exception);
        }
        finally
        {
            Volatile.Write(ref flushScheduled, 0);
            if (!pendingSamples.IsEmpty)
            {
                ScheduleFlush();
            }
        }
    }

    private static void WriteFallbackError(Exception exception)
    {
        try
        {
            var errorPath = Path.Combine(Path.GetTempPath(), FallbackErrorLogFileName);
            File.AppendAllText(
                errorPath,
                $"{DateTimeOffset.Now:O} {exception.GetType().Name}: {exception.Message}{Environment.NewLine}");
        }
        catch
        {
            Trace.WriteLine($"WuGesture diagnostics failed: {exception}");
        }
    }

    private static string FormatSample(GestureDiagnosticSample sample) => string.Format(
        CultureInfo.InvariantCulture,
        "{0:O} phase={1} total={2:F1}ms context={3:F1}ms exclusion={4:F1}ms fullscreen={5:F1}ms hook={6:F1}ms queue={7:F1}ms feedback={8:F1}ms action={9:F1}ms button={10} mode={11} target=0x{12:X} app={13} excluded={14} fullscreenChecked={15} fullscreenResult={16} {17}",
        sample.OccurredAt,
        sample.Phase,
        sample.TotalMilliseconds,
        sample.ContextMilliseconds,
        sample.ExclusionMilliseconds,
        sample.FullscreenMilliseconds,
        sample.HookMilliseconds,
        sample.QueueMilliseconds,
        sample.FeedbackMilliseconds,
        sample.ActionMilliseconds,
        sample.Button,
        sample.TargetWindowMode,
        sample.TargetWindow.ToInt64(),
        sample.AppName,
        sample.IsExcluded,
        sample.FullscreenChecked,
        sample.IsFullscreen,
        sample.Detail);
}

internal readonly record struct GestureDiagnosticSample(
    DateTimeOffset OccurredAt,
    string Phase,
    double TotalMilliseconds,
    double ContextMilliseconds,
    double ExclusionMilliseconds,
    double FullscreenMilliseconds,
    double HookMilliseconds,
    double QueueMilliseconds,
    double FeedbackMilliseconds,
    double ActionMilliseconds,
    GestureMouseButton? Button,
    string TargetWindowMode,
    IntPtr TargetWindow,
    string AppName,
    bool IsExcluded,
    bool IsFullscreen,
    bool FullscreenChecked,
    string Detail);
