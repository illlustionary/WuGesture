namespace WuGesture.App.Tests;

public sealed class CloseRequestPolicyTests
{
    [Theory]
    [InlineData(CloseReason.UserClosing, true)]
    [InlineData(CloseReason.TaskManagerClosing, true)]
    [InlineData(CloseReason.WindowsShutDown, false)]
    [InlineData(CloseReason.ApplicationExitCall, false)]
    [InlineData(CloseReason.MdiFormClosing, false)]
    [InlineData(CloseReason.FormOwnerClosing, false)]
    [InlineData(CloseReason.None, false)]
    public void IsCloseRequest_OnlyTreatsOutsideCloseRequestsAsUserCloses(CloseReason reason, bool expected)
    {
        Assert.Equal(expected, CloseRequestPolicy.IsCloseRequest(reason));
    }

    [Fact]
    public void IsCloseRequest_TreatsGestureCloseAsUserClose()
    {
        // A "close window" gesture posts WM_CLOSE to the target window. WinForms only reports
        // UserClosing for the title-bar button and Alt+F4; a posted WM_CLOSE arrives as
        // TaskManagerClosing, which used to bypass the configured close-button behavior.
        Assert.True(CloseRequestPolicy.IsCloseRequest(CloseReason.TaskManagerClosing));
    }
}
