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
    public static GestureRecognizerSettings Standard { get; } = new(
        GestureRuntimeDefaults.MinimumGestureDistance,
        GestureRuntimeDefaults.EffectiveMove,
        GestureRuntimeDefaults.DirectionTolerance,
        GestureRuntimeDefaults.TurnAngle,
        GestureRuntimeDefaults.MinimumTurnDistance);

    public static GestureRecognizerSettings Resolve(string? level)
    {
        return level switch
        {
            GestureConfigContract.GestureSensitivityLevels.Relaxed => new(
                32,
                18,
                45,
                45,
                10),
            GestureConfigContract.GestureSensitivityLevels.Strict => new(
                60,
                30,
                25,
                65,
                20),
            _ => Standard
        };
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
