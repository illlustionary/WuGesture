using System.Drawing;

namespace WuGesture.App.GestureEngine;

public sealed class GestureRecognizer
{
    private GestureRecognizerSettings settings = GestureSensitivityProfiles.Standard;

    public int MinimumGestureDistance => settings.MinimumGestureDistance;

    public void ApplySensitivity(string? level)
    {
        settings = GestureSensitivityProfiles.Resolve(level);
    }

    public IReadOnlyList<GestureDirection> Recognize(IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
        {
            return [];
        }

        var effectivePoints = BuildEffectivePoints(points);
        if (effectivePoints.Count < 2)
        {
            return [];
        }

        var directions = new List<GestureDirection>();
        var segmentStart = effectivePoints[0];
        var stableSegmentEnd = segmentStart;
        GestureDirection? activeDirection = null;
        Point? turnStart = null;

        for (var i = 1; i < effectivePoints.Count; i++)
        {
            var current = effectivePoints[i];
            if (activeDirection is null)
            {
                if (Distance(segmentStart, current) < settings.EffectiveMove)
                {
                    continue;
                }

                activeDirection = ToDirection(current.X - segmentStart.X, current.Y - segmentStart.Y, directions.Count);
                stableSegmentEnd = current;
                continue;
            }

            var activeAngle = DirectionToAngle(activeDirection.Value);
            var segmentAngle = ToAngle(segmentStart, current);
            if (AngleDistance(segmentAngle, activeAngle) <= settings.DirectionTolerance)
            {
                turnStart = null;
                stableSegmentEnd = current;
                continue;
            }

            turnStart ??= stableSegmentEnd;
            if (Distance(turnStart.Value, current) < settings.MinimumTurnDistance)
            {
                continue;
            }

            var turnAngle = ToAngle(turnStart.Value, current);
            if (AngleDistance(turnAngle, activeAngle) < settings.TurnAngle)
            {
                continue;
            }

            AddDirection(directions, CorrectFirstStrokeForMultiStroke(directions, activeDirection.Value, segmentStart, turnStart.Value));
            if (directions.Count >= GestureRuntimeDefaults.MaxGestureSteps)
            {
                return directions.ToArray();
            }

            segmentStart = turnStart.Value;
            stableSegmentEnd = current;
            activeDirection = ToDirection(current.X - segmentStart.X, current.Y - segmentStart.Y, directions.Count);
            turnStart = null;
        }

        if (activeDirection is not null)
        {
            AddDirection(directions, activeDirection.Value);
        }

        return directions.ToArray();
    }

    private List<Point> BuildEffectivePoints(IReadOnlyList<Point> points)
    {
        var effectivePoints = new List<Point> { points[0] };
        var lastEffectivePoint = points[0];

        for (var i = 1; i < points.Count; i++)
        {
            var current = points[i];
            if (Distance(lastEffectivePoint, current) < settings.EffectiveMove)
            {
                continue;
            }

            effectivePoints.Add(current);
            lastEffectivePoint = current;
        }

        var lastPoint = points[^1];
        if (lastPoint != lastEffectivePoint &&
            Distance(lastEffectivePoint, lastPoint) >= settings.MinimumTurnDistance)
        {
            effectivePoints.Add(lastPoint);
        }

        return effectivePoints;
    }

    private static void AddDirection(List<GestureDirection> directions, GestureDirection direction)
    {
        if (directions.Count == 0 || directions[^1] != direction)
        {
            directions.Add(direction);
        }
    }

    private static GestureDirection CorrectFirstStrokeForMultiStroke(
        List<GestureDirection> directions,
        GestureDirection direction,
        Point segmentStart,
        Point segmentEnd)
    {
        if (directions.Count != 0 || !IsDiagonal(direction))
        {
            return direction;
        }

        return ToCardinalDirection(segmentEnd.X - segmentStart.X, segmentEnd.Y - segmentStart.Y);
    }

    private static GestureDirection ToDirection(int dx, int dy, int completedStrokeCount)
    {
        return completedStrokeCount == 0
            ? ToEightDirection(dx, dy)
            : ToCardinalDirection(dx, dy);
    }

    private static GestureDirection ToEightDirection(int dx, int dy)
    {
        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        var normalized = NormalizePositiveAngle(angle + 22.5);
        var sector = (int)(normalized / 45.0) % 8;

        return sector switch
        {
            0 => GestureDirection.Right,
            1 => GestureDirection.DownRight,
            2 => GestureDirection.Down,
            3 => GestureDirection.DownLeft,
            4 => GestureDirection.Left,
            5 => GestureDirection.UpLeft,
            6 => GestureDirection.Up,
            _ => GestureDirection.UpRight
        };
    }

    private static GestureDirection ToCardinalDirection(int dx, int dy)
    {
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return dx >= 0 ? GestureDirection.Right : GestureDirection.Left;
        }

        return dy >= 0 ? GestureDirection.Down : GestureDirection.Up;
    }

    private static bool IsDiagonal(GestureDirection direction)
    {
        return direction is GestureDirection.UpRight
            or GestureDirection.DownRight
            or GestureDirection.DownLeft
            or GestureDirection.UpLeft;
    }

    private static double DirectionToAngle(GestureDirection direction)
    {
        return direction switch
        {
            GestureDirection.Right => 0.0,
            GestureDirection.DownRight => 45.0,
            GestureDirection.Down => 90.0,
            GestureDirection.DownLeft => 135.0,
            GestureDirection.Left => 180.0,
            GestureDirection.UpLeft => -135.0,
            GestureDirection.Up => -90.0,
            GestureDirection.UpRight => -45.0,
            _ => 0.0
        };
    }

    private static double ToAngle(Point start, Point end)
    {
        return Math.Atan2(end.Y - start.Y, end.X - start.X) * 180.0 / Math.PI;
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double AngleDistance(double angle, double expected)
    {
        return Math.Abs(NormalizeAngle(angle - expected));
    }

    private static double NormalizeAngle(double angle)
    {
        angle %= 360.0;
        if (angle > 180.0)
        {
            angle -= 360.0;
        }
        else if (angle < -180.0)
        {
            angle += 360.0;
        }

        return angle;
    }

    private static double NormalizePositiveAngle(double angle)
    {
        angle %= 360.0;
        if (angle < 0.0)
        {
            angle += 360.0;
        }

        return angle;
    }
}
