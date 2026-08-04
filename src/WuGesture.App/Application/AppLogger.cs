using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WuGesture.App;

internal enum AppLogLevel
{
    Information,
    Warning,
    Error
}

internal static class AppLogger
{
    private static readonly object syncRoot = new();
    private static AppLogWriter? writer;

    public static void Initialize()
    {
        lock (syncRoot)
        {
            writer ??= AppLogWriter.CreateDefault();
        }
    }

    public static void Information(string source, string eventName, string message) =>
        Write(AppLogLevel.Information, source, eventName, message, null);

    public static void Warning(string source, string eventName, string message, Exception? exception = null) =>
        Write(AppLogLevel.Warning, source, eventName, message, exception);

    public static void Error(string source, string eventName, string message, Exception exception) =>
        Write(AppLogLevel.Error, source, eventName, message, exception);

    public static void Shutdown(TimeSpan timeout)
    {
        AppLogWriter? currentWriter;
        lock (syncRoot)
        {
            currentWriter = writer;
            writer = null;
        }

        currentWriter?.Dispose(timeout);
    }

    private static void Write(AppLogLevel level, string source, string eventName, string message, Exception? exception)
    {
        var currentWriter = Volatile.Read(ref writer);
        if (currentWriter is null)
        {
            Trace.WriteLine($"WuGesture {level} {source}/{eventName}: {message}");
            return;
        }

        currentWriter.Write(level, source, eventName, message, exception);
    }
}

internal sealed class AppLogWriter : IDisposable
{
    private const int DefaultMaximumLogSizeBytes = 2 * 1024 * 1024;
    private const int DefaultRetainedDays = 7;
    private const int QueueCapacity = 2048;
    private const string LogFilePrefix = "application-";
    private const string FallbackErrorLogFileName = "WuGesture-application-log-error.log";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ConcurrentQueue<AppLogEntry> entries = new();
    private readonly SemaphoreSlim signal = new(0);
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task writerTask;
    private readonly string logDirectory;
    private readonly int maximumLogSizeBytes;
    private readonly int retainedDays;
    private int entryCount;
    private int droppedEntryCount;
    private int stopped;

    internal AppLogWriter(string logDirectory, int maximumLogSizeBytes = DefaultMaximumLogSizeBytes, int retainedDays = DefaultRetainedDays)
    {
        this.logDirectory = logDirectory;
        this.maximumLogSizeBytes = maximumLogSizeBytes;
        this.retainedDays = retainedDays;
        writerTask = Task.Run(WriteLoopAsync);
    }

    internal static AppLogWriter CreateDefault()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppIdentity.AppDataFolderName,
            "logs");
        return new AppLogWriter(directory);
    }

    internal void Write(AppLogLevel level, string source, string eventName, string message, Exception? exception = null)
    {
        if (Volatile.Read(ref stopped) != 0 || !TryReserveEntry())
        {
            return;
        }

        entries.Enqueue(new AppLogEntry(
            DateTimeOffset.UtcNow,
            level,
            source,
            eventName,
            message,
            Environment.ProcessId,
            Environment.CurrentManagedThreadId,
            Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
            exception?.GetType().FullName,
            exception?.Message,
            exception?.StackTrace));
        signal.Release();
    }

    internal void Dispose(TimeSpan timeout)
    {
        if (Interlocked.Exchange(ref stopped, 1) != 0)
        {
            return;
        }

        cancellation.Cancel();
        signal.Release();
        try
        {
            writerTask.Wait(timeout);
        }
        catch (Exception exception)
        {
            WriteFallbackError(exception);
        }
        finally
        {
            cancellation.Dispose();
            signal.Dispose();
        }
    }

    public void Dispose() => Dispose(TimeSpan.FromSeconds(2));

    private bool TryReserveEntry()
    {
        while (true)
        {
            var currentCount = Volatile.Read(ref entryCount);
            if (currentCount >= QueueCapacity)
            {
                Interlocked.Increment(ref droppedEntryCount);
                return false;
            }

            if (Interlocked.CompareExchange(ref entryCount, currentCount + 1, currentCount) == currentCount)
            {
                return true;
            }
        }
    }

    private async Task WriteLoopAsync()
    {
        try
        {
            while (true)
            {
                try
                {
                    await signal.WaitAsync(cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                }

                FlushEntries();
                if (Volatile.Read(ref stopped) != 0 && entries.IsEmpty)
                {
                    return;
                }
            }
        }
        catch (Exception exception)
        {
            WriteFallbackError(exception);
        }
    }

    private void FlushEntries()
    {
        var batch = new List<AppLogEntry>();
        while (entries.TryDequeue(out var entry))
        {
            Interlocked.Decrement(ref entryCount);
            batch.Add(entry);
        }

        var droppedCount = Interlocked.Exchange(ref droppedEntryCount, 0);
        if (droppedCount > 0)
        {
            batch.Add(new AppLogEntry(
                DateTimeOffset.UtcNow,
                AppLogLevel.Warning,
                "AppLogger",
                "entries-dropped",
                $"Dropped {droppedCount} log entries because the queue was full.",
                Environment.ProcessId,
                Environment.CurrentManagedThreadId,
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
                null,
                null,
                null));
        }

        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, $"{LogFilePrefix}{DateTime.UtcNow:yyyyMMdd}.log");
            RotateIfNeeded(logPath);
            File.AppendAllLines(logPath, batch.Select(SerializeEntry));
            DeleteExpiredLogs();
        }
        catch (Exception exception)
        {
            WriteFallbackError(exception);
        }
    }

    private void RotateIfNeeded(string logPath)
    {
        if (!File.Exists(logPath) || new FileInfo(logPath).Length < maximumLogSizeBytes)
        {
            return;
        }

        File.Move(logPath, logPath + ".previous", overwrite: true);
    }

    private void DeleteExpiredLogs()
    {
        var oldestAllowedTime = DateTime.UtcNow.AddDays(-retainedDays);
        foreach (var filePath in Directory.EnumerateFiles(logDirectory, $"{LogFilePrefix}*.log*"))
        {
            if (File.GetLastWriteTimeUtc(filePath) < oldestAllowedTime)
            {
                File.Delete(filePath);
            }
        }
    }

    private static string SerializeEntry(AppLogEntry entry) => JsonSerializer.Serialize(entry, JsonOptions);

    private static void WriteFallbackError(Exception exception)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(Path.GetTempPath(), FallbackErrorLogFileName),
                $"{DateTimeOffset.UtcNow:O} {exception}{Environment.NewLine}");
        }
        catch
        {
            Trace.WriteLine($"WuGesture application logging failed: {exception}");
        }
    }
}

internal sealed record AppLogEntry(
    DateTimeOffset Timestamp,
    AppLogLevel Level,
    string Source,
    string EventName,
    string Message,
    int ProcessId,
    int ThreadId,
    string Version,
    string? ExceptionType,
    string? ExceptionMessage,
    string? ExceptionStackTrace);
