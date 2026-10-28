using System.Drawing;
using WuGesture.App.GestureEngine;

namespace WuGesture.App.Tests;

public sealed class GestureSessionTests
{
    [Fact]
    public void Start_ResetsTheSessionAndCollectsTheStartPoint()
    {
        var session = new GestureSession();

        session.Start(42, new Point(10, 20));

        Assert.Equal(42, session.Id);
        Assert.True(session.IsTracking);
        Assert.False(session.IsCompleting);
        Assert.Null(session.PreviewActionName);
        var point = Assert.Single(session.Points);
        Assert.Equal(new Point(10, 20), point);
    }

    [Fact]
    public void BeginCompleting_LeavesOnlyTheMatchingSessionCompletable()
    {
        var session = new GestureSession();
        session.Start(7, new Point(0, 0));
        session.Append(new Point(5, 5));

        session.BeginCompleting();

        Assert.False(session.IsTracking);
        Assert.True(session.IsCompleting);
        Assert.True(session.CanComplete(7));
        Assert.False(session.CanComplete(8));

        session.Complete(8);
        Assert.Equal(7, session.Id);

        session.Complete(7);
        Assert.Equal(0, session.Id);
        Assert.False(session.IsCompleting);
        Assert.Empty(session.Points);
    }

    [Fact]
    public void Cancel_ClearsTheSession()
    {
        var session = new GestureSession();
        session.Start(3, new Point(1, 1));
        session.Append(new Point(2, 2));

        session.Cancel();

        Assert.Equal(0, session.Id);
        Assert.False(session.IsTracking);
        Assert.False(session.IsCompleting);
        Assert.Empty(session.Points);
    }

    [Fact]
    public void ShouldPublishProgress_OnlyReportsChanges()
    {
        var session = new GestureSession();
        session.Start(1, new Point(0, 0));
        var path = new List<Point> { new(0, 0) };
        var pattern = new List<GestureDirection> { GestureDirection.Down };

        Assert.True(session.ShouldPublishProgress(pattern, path, force: false));
        Assert.False(session.ShouldPublishProgress(pattern, path, force: false));
        Assert.True(session.ShouldPublishProgress(pattern, path, force: true));

        pattern.Add(GestureDirection.Right);

        Assert.True(session.ShouldPublishProgress(pattern, path, force: false));
    }

    [Fact]
    public void ShouldPublishProgress_KeepsItsOwnSnapshot()
    {
        var session = new GestureSession();
        session.Start(1, new Point(0, 0));
        var path = new List<Point> { new(0, 0) };
        var pattern = new List<GestureDirection> { GestureDirection.Down };
        session.ShouldPublishProgress(pattern, path, force: false);

        path.Add(new Point(1, 1));
        pattern.Add(GestureDirection.Left);

        Assert.False(session.ShouldPublishProgress(
            new List<GestureDirection> { GestureDirection.Down },
            new List<Point> { new(0, 0) },
            force: false));
    }

    [Fact]
    public void ClearPreview_ReturnsAndResetsThePreviewSession()
    {
        var session = new GestureSession();
        session.PreviewActionName = "最大化";
        session.PreviewSessionId = 9;

        Assert.Equal(9, session.ClearPreview());
        Assert.Null(session.PreviewActionName);
        Assert.Equal(0, session.PreviewSessionId);

        session.PreviewActionName = "最小化";
        session.PreviewSessionId = 9;

        Assert.Equal(4, session.ClearPreview(4));
        Assert.Null(session.PreviewActionName);
        Assert.Equal(0, session.PreviewSessionId);
    }
}
