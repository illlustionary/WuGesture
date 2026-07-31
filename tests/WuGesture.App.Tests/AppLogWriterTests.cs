using System.Text.Json;

namespace WuGesture.App.Tests;

public sealed class AppLogWriterTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"WuGesture.Tests.{Guid.NewGuid():N}");

    [Fact]
    public void Dispose_WritesStructuredEntries()
    {
        using var writer = new AppLogWriter(directory, maximumLogSizeBytes: 1024, retainedDays: 7);
        writer.Write(AppLogLevel.Error, "Tests", "write-failed", "Operation failed.", new InvalidOperationException("expected"));

        writer.Dispose(TimeSpan.FromSeconds(2));

        var logFile = Assert.Single(Directory.GetFiles(directory, "application-*.log"));
        using var document = JsonDocument.Parse(File.ReadAllText(logFile));
        var root = document.RootElement;
        Assert.Equal("Error", root.GetProperty("Level").GetString());
        Assert.Equal("Tests", root.GetProperty("Source").GetString());
        Assert.Equal("write-failed", root.GetProperty("EventName").GetString());
        Assert.Equal(typeof(InvalidOperationException).FullName, root.GetProperty("ExceptionType").GetString());
    }

    [Fact]
    public void Write_RotatesExistingLogAtConfiguredSize()
    {
        Directory.CreateDirectory(directory);
        var logPath = Path.Combine(directory, $"application-{DateTime.UtcNow:yyyyMMdd}.log");
        File.WriteAllText(logPath, new string('x', 128));

        using var writer = new AppLogWriter(directory, maximumLogSizeBytes: 64, retainedDays: 7);
        writer.Write(AppLogLevel.Information, "Tests", "rotated", "A new entry.");
        writer.Dispose(TimeSpan.FromSeconds(2));

        Assert.True(File.Exists(logPath + ".previous"));
        Assert.Contains("rotated", File.ReadAllText(logPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
