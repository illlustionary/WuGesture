namespace WuGesture.App.GestureEngine;

internal static class GestureRuntimeDefaults
{
    public const int MinimumPointDistance = 3;
    public const int MinimumGestureDistance = 45;
    public const double EffectiveMove = 24.0;
    public const double DiagonalTolerance = 22.5;
    public const double DirectionTolerance = 35.0;
    public const double TurnAngle = 55.0;
    public const double MinimumTurnDistance = 15.0;
    public const int MaxGestureSteps = 12;
}

internal readonly record struct GestureRecognizerSettings(
    int MinimumGestureDistance,
    double EffectiveMove,
    double DirectionTolerance,
    double TurnAngle,
    double MinimumTurnDistance);

internal static class GestureSensitivityProfiles
{
    private static GestureRecognizerSettings Strict { get; } = new(
        70,
        36,
        25,
        70,
        24);

    internal static GestureRecognizerSettings Standard { get; } = new(
        GestureRuntimeDefaults.MinimumGestureDistance,
        GestureRuntimeDefaults.EffectiveMove,
        GestureRuntimeDefaults.DirectionTolerance,
        GestureRuntimeDefaults.TurnAngle,
        GestureRuntimeDefaults.MinimumTurnDistance);

    private static GestureRecognizerSettings Relaxed { get; } = new(
        18,
        8,
        55,
        40,
        4);

    public static GestureRecognizerSettings Resolve(int percent)
    {
        var normalized = Math.Clamp(percent, 0, 200);
        if (normalized <= 100)
        {
            return Interpolate(Strict, Standard, normalized / 100d);
        }

        return Interpolate(Standard, Relaxed, (normalized - 100) / 100d);
    }

    private static GestureRecognizerSettings Interpolate(
        GestureRecognizerSettings from,
        GestureRecognizerSettings to,
        double amount)
    {
        return new GestureRecognizerSettings(
            InterpolateInt(from.MinimumGestureDistance, to.MinimumGestureDistance, amount),
            InterpolateDouble(from.EffectiveMove, to.EffectiveMove, amount),
            InterpolateDouble(from.DirectionTolerance, to.DirectionTolerance, amount),
            InterpolateDouble(from.TurnAngle, to.TurnAngle, amount),
            InterpolateDouble(from.MinimumTurnDistance, to.MinimumTurnDistance, amount));
    }

    private static int InterpolateInt(int from, int to, double amount)
    {
        return (int)Math.Round(from + (to - from) * amount);
    }

    private static double InterpolateDouble(double from, double to, double amount)
    {
        return from + (to - from) * amount;
    }
}

internal static class EdgeActionRuntimeDefaults
{
    public const int EdgeThickness = 3;
    public const int CornerSize = 18;
    public const int FrictionEdgeThickness = 16;
    public const int FrictionCornerExcludeSize = 100;
    public const int FrictionStepPixels = 60;
    public const int FrictionResetDistance = 50;
    public const int MousePollIntervalMs = 16;
    public static readonly TimeSpan FrictionMoveTimeout = TimeSpan.FromMilliseconds(1200);
    public static readonly TimeSpan FrictionTriggerResetTimeout = TimeSpan.FromMilliseconds(1500);
}
