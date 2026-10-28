using System.Drawing;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureInputCaptureTests
{
    private static MouseHookEventArgs At(int x, int y) => new(new Point(x, y));

    [Fact]
    public void HandleButtonDown_CapturesTheButtonAndRaisesStart()
    {
        var capture = new GestureInputCapture(_ => true);
        var started = new List<(Point Location, GestureInputButton Button)>();
        capture.GestureStarted += (location, button, _, _) => started.Add((location, button));

        var args = At(10, 20);

        capture.HandleButtonDown(args, GestureInputButton.Middle);

        Assert.True(args.Handled);
        var start = Assert.Single(started);
        Assert.Equal(new Point(10, 20), start.Location);
        Assert.Equal(GestureInputButton.Middle, start.Button);
    }

    [Fact]
    public void HandleButtonDown_IsIgnoredWhenTheGateRejectsTheLocation()
    {
        var capture = new GestureInputCapture(_ => false);
        var started = 0;
        capture.GestureStarted += (_, _, _, _) => started++;

        var args = At(0, 0);

        capture.HandleButtonDown(args, GestureInputButton.Right);

        Assert.False(args.Handled);
        Assert.Equal(0, started);
    }

    [Fact]
    public void SetPaused_StopsCapturingUntilItIsTurnedOff()
    {
        var capture = new GestureInputCapture(_ => true);
        capture.SetPaused(true);

        var pausedRelease = At(0, 0);
        capture.HandleButtonDown(pausedRelease, GestureInputButton.Right);
        Assert.False(pausedRelease.Handled);

        capture.SetPaused(false);
        var resumedRelease = At(0, 0);
        capture.HandleButtonDown(resumedRelease, GestureInputButton.Right);
        Assert.True(resumedRelease.Handled);
    }

    [Fact]
    public void SetRecordingRequest_AllowsCapturingWhilePaused()
    {
        var capture = new GestureInputCapture(_ => false);
        capture.SetPaused(true);
        capture.SetRecordingRequest("recording-1");
        string? receivedId = null;
        capture.GestureStarted += (_, _, _, id) => receivedId = id;

        var args = At(3, 4);

        capture.HandleButtonDown(args, GestureInputButton.Right);

        Assert.True(args.Handled);
        Assert.Equal("recording-1", receivedId);
    }

    [Fact]
    public void HandleMove_OnlyRaisesWhileAButtonIsDown()
    {
        var capture = new GestureInputCapture(_ => true);
        var moves = new List<Point>();
        capture.GestureMoved += (location, _) => moves.Add(location);

        capture.HandleMove(At(1, 1));
        Assert.Empty(moves);

        capture.HandleButtonDown(At(2, 2), GestureInputButton.Right);
        capture.HandleMove(At(3, 3));

        var move = Assert.Single(moves);
        Assert.Equal(new Point(3, 3), move);
    }

    [Fact]
    public void HandleButtonUp_RaisesEndAndConsumesTheRelease()
    {
        var capture = new GestureInputCapture(_ => true);
        GestureInputButton? ended = null;
        capture.GestureEnded += (_, button) => ended = button;

        capture.HandleButtonDown(At(0, 0), GestureInputButton.Right);
        var releaseArgs = At(5, 5);

        capture.HandleButtonUp(releaseArgs, GestureInputButton.Right);

        Assert.True(releaseArgs.Handled);
        Assert.Equal(GestureInputButton.Right, ended!.Value);
    }

    [Fact]
    public void SetPaused_DuringTracking_SwallowsThePendingRelease()
    {
        var capture = new GestureInputCapture(_ => true);
        var endedCount = 0;
        capture.GestureEnded += (_, _) => endedCount++;

        capture.HandleButtonDown(At(0, 0), GestureInputButton.Right);
        capture.SetPaused(true);

        var releaseArgs = At(0, 0);
        capture.HandleButtonUp(releaseArgs, GestureInputButton.Right);

        Assert.True(releaseArgs.Handled);
        Assert.Equal(0, endedCount);

        capture.SetPaused(false);
        var secondDown = At(0, 0);
        capture.HandleButtonDown(secondDown, GestureInputButton.Right);
        Assert.True(secondDown.Handled);
    }

    [Fact]
    public void HandleButtonDown_WithAnotherButton_SwallowsThePreviousRelease()
    {
        var capture = new GestureInputCapture(_ => true);
        var ended = new List<GestureInputButton>();
        capture.GestureEnded += (_, button) => ended.Add(button);

        capture.HandleButtonDown(At(0, 0), GestureInputButton.Right);
        capture.HandleButtonDown(At(0, 0), GestureInputButton.Middle);

        var rightRelease = At(0, 0);
        capture.HandleButtonUp(rightRelease, GestureInputButton.Right);
        Assert.True(rightRelease.Handled);
        Assert.Empty(ended);

        var middleRelease = At(0, 0);
        capture.HandleButtonUp(middleRelease, GestureInputButton.Middle);
        Assert.True(middleRelease.Handled);
        var endedButton = Assert.Single(ended);
        Assert.Equal(GestureInputButton.Middle, endedButton);
    }

    [Fact]
    public void Dispose_StopsCapturing()
    {
        var capture = new GestureInputCapture(_ => true);
        capture.Dispose();

        var args = At(0, 0);

        capture.HandleButtonDown(args, GestureInputButton.Right);

        Assert.False(args.Handled);
    }
}
