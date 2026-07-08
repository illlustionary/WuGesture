namespace MyGesture.App.GestureEngine;

internal static class GestureRuntimeDefaults
{
    public const int MinimumPointDistance = 3;
    public const int MinimumGestureDistance = 45;
    public const double EffectiveMove = 24.0;
    public const double DiagonalTolerance = 22.5;
    public const int MaxGestureSteps = 12;
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
