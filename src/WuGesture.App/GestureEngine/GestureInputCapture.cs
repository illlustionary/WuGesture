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
    private bool wheelCaptureAllowed;
    private long suppressWheelUntilTick;
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
                wheelCaptureAllowed = false;
            }
        }
    }

    public void SetRecordingRequest(string? requestId)
    {
        lock (stateLock)
        {
            recordingRequestId = requestId;
            if (requestId is not null)
            {
                wheelCaptureAllowed = false;
            }
        }
    }

    /// <summary>
    /// True while a held gesture button may consume wheel notches. Like WGestures, any wheel
    /// input during a held gesture belongs to the gesture and must not scroll the target
    /// application, even when no wheel-modifier rule is configured. The parser turns this off
    /// when the session ends or never starts (excluded app, fullscreen, recording, pause).
    /// </summary>
    public void SetWheelCaptureAllowed(bool allowed)
    {
        lock (stateLock)
        {
            wheelCaptureAllowed = allowed && !disposed;
        }
    }

    public bool ShouldCaptureWheel()
    {
        lock (stateLock)
        {
            return !disposed &&
                   !isPaused &&
                   recordingRequestId is null &&
                   activeButton != GestureInputButton.None &&
                   wheelCaptureAllowed;
        }
    }

    /// <summary>
    /// Wheel events that arrive shortly after a gesture ends are swallowed without being
    /// dispatched. This prevents leftover scroll from a middle-button gesture or its injected
    /// shortcut from leaking into the target application (for example Ctrl+wheel zoom).
    /// </summary>
    public void SuppressWheelFor(TimeSpan duration)
    {
        lock (stateLock)
        {
            suppressWheelUntilTick = Environment.TickCount64 + (long)duration.TotalMilliseconds;
        }
    }

    public bool ShouldSuppressWheel()
    {
        lock (stateLock)
        {
            return !disposed &&
                   !isPaused &&
                   activeButton == GestureInputButton.None &&
                   Environment.TickCount64 < suppressWheelUntilTick;
        }
    }

    public void Dispose()
    {
        lock (stateLock)
        {
            disposed = true;
            activeButton = GestureInputButton.None;
            suppressedReleaseButton = GestureInputButton.None;
            wheelCaptureAllowed = false;
            suppressWheelUntilTick = 0;
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
            wheelCaptureAllowed = true;
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
                wheelCaptureAllowed = false;
                e.Handled = true;
                return;
            }

            if (disposed || activeButton != button)
            {
                return;
            }

            activeButton = GestureInputButton.None;
            wheelCaptureAllowed = false;
            e.Handled = true;
        }

        GestureEnded?.Invoke(e.Location, button);
    }
}
