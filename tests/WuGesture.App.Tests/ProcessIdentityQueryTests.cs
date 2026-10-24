using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class ProcessIdentityQueryTests
{
    [Fact]
    public void TryGetIdentity_ReadsCurrentProcessNameAndPath()
    {
        var processId = Environment.ProcessId;

        Assert.True(ProcessImageQuery.TryGetIdentity(processId, out var processName, out var executablePath));

        Assert.False(string.IsNullOrWhiteSpace(processName));
        Assert.False(string.IsNullOrWhiteSpace(executablePath));
        Assert.True(File.Exists(executablePath));
        Assert.Equal(
            Path.GetFileNameWithoutExtension(executablePath),
            processName,
            ignoreCase: true);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryGetIdentity_ReturnsFalseForInvalidProcessId(int processId)
    {
        Assert.False(ProcessImageQuery.TryGetIdentity(processId, out var processName, out var executablePath));
        Assert.Empty(processName);
        Assert.Empty(executablePath);
    }

    [Fact]
    public void WindowProcessCache_ReturnsFalseForEmptyWindow()
    {
        Assert.False(WindowProcessCache.TryGet(IntPtr.Zero, includeExecutablePath: true, out _));
    }
}
