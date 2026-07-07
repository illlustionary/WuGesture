using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureRecognizer
{
    private const double EffectiveMove = 24.0;
    private const double DiagonalTolerance = 22.5;
    private const int MaxGestureSteps = 12;

    public IReadOnlyList<GestureDirection> Recognize(IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
        {
            return [];
        }

        var directions = new List<GestureDirection>();
        var lastEffectivePoint = points[0];

        for (var i = 1; i < points.Count; i++)
        {
            var current = points[i];
            if (Distance(lastEffectivePoint, current) < EffectiveMove)
            {
                continue;
            }

            var direction = ToCardinalDirection(
                current.X - lastEffectivePoint.X,
                current.Y - lastEffectivePoint.Y);

            if (directions.Count == 0 || directions[^1] != direction)
            {
                directions.Add(direction);
                if (directions.Count >= MaxGestureSteps)
                {
                    break;
                }
            }

            lastEffectivePoint = current;
        }

        if (directions.Count == 1 &&
            TryGetSingleStrokeDiagonal(points[0], points[^1], out var diagonalDirection))
        {
            directions[0] = diagonalDirection;
        }

        return directions.ToArray();
    }

    private static GestureDirection ToCardinalDirection(int dx, int dy)
    {
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return dx > 0 ? GestureDirection.Right : GestureDirection.Left;
        }

        return dy > 0 ? GestureDirection.Down : GestureDirection.Up;
    }

    private static bool TryGetSingleStrokeDiagonal(Point start, Point end, out GestureDirection direction)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;

        if (IsNearAngle(angle, -135.0) || IsNearAngle(angle, 225.0))
        {
            direction = GestureDirection.UpLeft;
            return true;
        }

        if (IsNearAngle(angle, -45.0) || IsNearAngle(angle, 315.0))
        {
            direction = GestureDirection.UpRight;
            return true;
        }

        if (IsNearAngle(angle, 45.0))
        {
            direction = GestureDirection.DownRight;
            return true;
        }

        if (IsNearAngle(angle, 135.0))
        {
            direction = GestureDirection.DownLeft;
            return true;
        }

        direction = default;
        return false;
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool IsNearAngle(double angle, double expected)
    {
        return Math.Abs(NormalizeAngle(angle - expected)) <= DiagonalTolerance;
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
}
