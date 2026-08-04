using System.Diagnostics;
using System.Drawing;

namespace WuGesture.App.GestureEngine;

internal enum GestureInputButton
{
    None,
    Right,
    Middle
}

internal sealed class GestureInputCapture
{
    private readonly object stateLock = new();
    private readonly Func<Point, bool> canCaptureNormalGesture;
    private GestureInputButton activeButton;
    private GestureInputButton suppressedReleaseButton;
    private bool isPaused;
    private string? recordingRequestId;
    private bool disposed;

    public GestureInputCapture(Func<Point, bool> canCaptureNormalGesture)
    {
        this.canCaptureNormalGesture = canCaptureNormalGesture;
    }

    public event Action<Point, GestureInputButton, long, string?>? GestureStarted;

    public event Action<Point, long>? GestureMoved;

    public event Action<Point, GestureInputButton>? GestureEnded;

    public void SetPaused(bool paused)
    {
        lock (stateLock)
        {
            if (disposed || isPaused == paused)
            {
                return;
            }

            isPaused = paused;
            if (paused && activeButton != GestureInputButton.None)
            {
                suppressedReleaseButton = activeButton;
                activeButton = GestureInputButton.None;
            }
        }
    }

    public void SetRecordingRequest(string? requestId)
    {
        lock (stateLock)
        {
            recordingRequestId = requestId;
        }
    }

    public void Dispose()
    {
        lock (stateLock)
        {
            disposed = true;
            activeButton = GestureInputButton.None;
            suppressedReleaseButton = GestureInputButton.None;
            recordingRequestId = null;
        }
    }

    public void HandleButtonDown(MouseHookEventArgs e, GestureInputButton button)
    {
        string? recordingId;
        lock (stateLock)
        {
            if (disposed || (isPaused && recordingRequestId is null))
            {
                return;
            }

            recordingId = recordingRequestId;
        }

        if (recordingId is null && !canCaptureNormalGesture(e.Location))
        {
            return;
        }

        lock (stateLock)
        {
            if (disposed || (isPaused && recordingRequestId is null))
            {
                return;
            }

            recordingId = recordingRequestId;
            if (activeButton != GestureInputButton.None && activeButton != button)
            {
                suppressedReleaseButton = activeButton;
            }

            activeButton = button;
            if (suppressedReleaseButton == button)
            {
                suppressedReleaseButton = GestureInputButton.None;
            }
        }

        e.Handled = true;
        GestureStarted?.Invoke(e.Location, button, Stopwatch.GetTimestamp(), recordingId);
    }

    public void HandleMove(MouseHookEventArgs e)
    {
        lock (stateLock)
        {
            if (disposed || activeButton == GestureInputButton.None)
            {
                return;
            }
        }

        GestureMoved?.Invoke(e.Location, Stopwatch.GetTimestamp());
    }

    public void HandleButtonUp(MouseHookEventArgs e, GestureInputButton button)
    {
        lock (stateLock)
        {
            if (suppressedReleaseButton == button)
            {
                suppressedReleaseButton = GestureInputButton.None;
                e.Handled = true;
                return;
            }

            if (disposed || activeButton != button)
            {
                return;
            }

            activeButton = GestureInputButton.None;
            e.Handled = true;
        }

        GestureEnded?.Invoke(e.Location, button);
    }
}
