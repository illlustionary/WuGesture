using System.Xml.Linq;

namespace WuGesture.App.Tests;

public sealed class ScheduledTaskLauncherTests
{
    private const string ExecutablePath = @"C:\Program Files\WuGesture\WuGesture.exe";
    private const string UserSid = "S-1-5-21-1111-2222-3333-1001";
    private static readonly XNamespace TaskNamespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";

    [Fact]
    public void BuildTaskXml_RegistersAnOnDemandHighestPrivilegesTask()
    {
        var xml = ScheduledTaskLauncher.BuildTaskXml(ExecutablePath, UserSid);
        var document = XDocument.Parse(xml);

        Assert.NotNull(document.Root);
        Assert.Equal("1.2", document.Root!.Attribute("version")?.Value);
        Assert.Empty(document.Descendants(TaskNamespace + "Triggers").Elements());
        Assert.Equal(UserSid, Value(document, "UserId"));
        Assert.Equal("InteractiveToken", Value(document, "LogonType"));
        Assert.Equal("HighestAvailable", Value(document, "RunLevel"));
        Assert.Equal("true", Value(document, "AllowStartOnDemand"));
        Assert.Equal(ScheduledTaskLauncher.TaskName, Value(document, "URI").TrimStart('\\'));
    }

    [Fact]
    public void BuildTaskXml_UsesAFixedCommandLineAndNoTimeLimit()
    {
        var document = XDocument.Parse(ScheduledTaskLauncher.BuildTaskXml(ExecutablePath, UserSid));

        Assert.Equal(ExecutablePath, Value(document, "Command"));
        Assert.Equal(AppIdentity.ScheduledLaunchArgument, Value(document, "Arguments"));
        Assert.Equal(@"C:\Program Files\WuGesture", Value(document, "WorkingDirectory"));
        // Gesture apps stay in the tray for weeks, so the default 72h limit must not be used.
        Assert.Equal("PT0S", Value(document, "ExecutionTimeLimit"));
    }

    [Fact]
    public void ClassifyXml_ComparesTheRegisteredCommandWithTheExpectedExecutable()
    {
        var xml = ScheduledTaskLauncher.BuildTaskXml(ExecutablePath, UserSid);

        Assert.Equal(ScheduledTaskState.Matches, ScheduledTaskLauncher.ClassifyXml(xml, @"c:\program files\wugesture\WUGESTURE.EXE"));
        Assert.Equal(ScheduledTaskState.PointsElsewhere, ScheduledTaskLauncher.ClassifyXml(xml, @"D:\Portable\WuGesture\WuGesture.exe"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ClassifyXml_TreatsAnEmptyTaskAsMissing(string xml)
    {
        Assert.Equal(ScheduledTaskState.Missing, ScheduledTaskLauncher.ClassifyXml(xml, ExecutablePath));
    }

    [Fact]
    public void ClassifyXml_TreatsAnUnparsableTaskAsUnknown()
    {
        Assert.Equal(
            ScheduledTaskState.Unknown,
            ScheduledTaskLauncher.ClassifyXml("<Task>", ExecutablePath));
    }

    [Fact]
    public void ClassifyXml_TreatsATaskWithoutACommandAsPointingElsewhere()
    {
        const string xml = """
            <Task xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task" version="1.2">
              <Actions Context="Author">
                <Exec />
              </Actions>
            </Task>
            """;

        Assert.Equal(ScheduledTaskState.PointsElsewhere, ScheduledTaskLauncher.ClassifyXml(xml, ExecutablePath));
    }

    private static string Value(XDocument document, string elementName)
    {
        return document.Descendants(TaskNamespace + elementName).First().Value;
    }
}
