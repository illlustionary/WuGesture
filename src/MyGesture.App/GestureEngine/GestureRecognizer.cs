using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureRecognizer
{
    public IReadOnlyList<GestureDirection> Recognize(IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
        {
            return [];
        }

        var directions = new List<GestureDirection>();
        var segmentStart = points[0];
        GestureDirection? activeDirection = null;
        Point? turnStart = null;

        for (var i = 1; i < points.Count; i++)
        {
            var current = points[i];
            if (activeDirection is null)
            {
                if (Distance(segmentStart, current) < GestureRuntimeDefaults.EffectiveMove)
                {
                    continue;
                }

                activeDirection = ToDirection(current.X - segmentStart.X, current.Y - segmentStart.Y);
                continue;
            }

            var activeAngle = DirectionToAngle(activeDirection.Value);
            var segmentAngle = ToAngle(segmentStart, current);
            if (AngleDistance(segmentAngle, activeAngle) <= GestureRuntimeDefaults.DirectionTolerance)
            {
                turnStart = null;
                continue;
            }

            turnStart ??= points[i - 1];
            if (Distance(turnStart.Value, current) < GestureRuntimeDefaults.MinimumTurnDistance)
            {
                continue;
            }

            var turnAngle = ToAngle(turnStart.Value, current);
            if (AngleDistance(turnAngle, activeAngle) < GestureRuntimeDefaults.TurnAngle)
            {
                continue;
            }

            AddDirection(directions, activeDirection.Value);
            if (directions.Count >= GestureRuntimeDefaults.MaxGestureSteps)
            {
                return directions.ToArray();
            }

            segmentStart = turnStart.Value;
            activeDirection = ToDirection(current.X - segmentStart.X, current.Y - segmentStart.Y);
            turnStart = null;
        }

        if (activeDirection is not null)
        {
            AddDirection(directions, activeDirection.Value);
        }

        return directions.ToArray();
    }

    private static void AddDirection(List<GestureDirection> directions, GestureDirection direction)
    {
        if (directions.Count == 0 || directions[^1] != direction)
        {
            directions.Add(direction);
        }
    }

    private static GestureDirection ToDirection(int dx, int dy)
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
