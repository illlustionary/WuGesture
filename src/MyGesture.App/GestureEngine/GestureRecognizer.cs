using System.Drawing;

namespace MyGesture.App.GestureEngine;

public sealed class GestureRecognizer
{
    private const double SectorSize = 45.0;
    private const double SimplifyTolerance = 18.0;
    private const double MinimumPointSpacing = 8.0;
    private const double MinimumStrokeLength = 24.0;
    private const double ShortStrokeLength = 18.0;
    private const int MaxGestureSteps = 12;

    public IReadOnlyList<GestureDirection> Recognize(IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
        {
            return [];
        }

        var cleanedPoints = RemoveClosePoints(points);
        if (cleanedPoints.Count < 2)
        {
            return [];
        }

        var simplifiedPoints = Simplify(cleanedPoints, SimplifyTolerance);
        var strokes = ToStrokes(simplifiedPoints);
        if (strokes.Count == 0)
        {
            return [];
        }

        strokes = MergeSameDirections(strokes);
        strokes = RemoveNoise(strokes);
        strokes = MergeSameDirections(strokes);

        return strokes
            .Take(MaxGestureSteps)
            .Select(stroke => stroke.Direction)
            .ToArray();
    }

    private static List<Point> RemoveClosePoints(IReadOnlyList<Point> points)
    {
        var cleaned = new List<Point> { points[0] };

        for (var i = 1; i < points.Count; i++)
        {
            if (Distance(cleaned[^1], points[i]) >= MinimumPointSpacing)
            {
                cleaned.Add(points[i]);
            }
        }

        if (cleaned[^1] != points[^1])
        {
            cleaned.Add(points[^1]);
        }

        return cleaned;
    }

    private static List<Point> Simplify(IReadOnlyList<Point> points, double tolerance)
    {
        if (points.Count <= 2)
        {
            return points.ToList();
        }

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;

        SimplifySection(points, 0, points.Count - 1, tolerance, keep);

        var simplified = new List<Point>();
        for (var i = 0; i < points.Count; i++)
        {
            if (keep[i])
            {
                simplified.Add(points[i]);
            }
        }

        return simplified;
    }

    private static void SimplifySection(
        IReadOnlyList<Point> points,
        int startIndex,
        int endIndex,
        double tolerance,
        bool[] keep)
    {
        var maxDistance = 0.0;
        var index = startIndex;

        for (var i = startIndex + 1; i < endIndex; i++)
        {
            var distance = PerpendicularDistance(points[i], points[startIndex], points[endIndex]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                index = i;
            }
        }

        if (maxDistance < tolerance)
        {
            return;
        }

        keep[index] = true;
        SimplifySection(points, startIndex, index, tolerance, keep);
        SimplifySection(points, index, endIndex, tolerance, keep);
    }

    private static List<DirectionStroke> ToStrokes(IReadOnlyList<Point> points)
    {
        var strokes = new List<DirectionStroke>();

        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1];
            var current = points[i];
            var dx = current.X - previous.X;
            var dy = current.Y - previous.Y;
            var length = Distance(previous, current);

            if (length < ShortStrokeLength)
            {
                continue;
            }

            var direction = ToDirection(dx, dy);
            strokes.Add(new DirectionStroke(direction, length));
        }

        return strokes;
    }

    private static List<DirectionStroke> MergeSameDirections(IReadOnlyList<DirectionStroke> strokes)
    {
        if (strokes.Count == 0)
        {
            return [];
        }

        var merged = new List<DirectionStroke> { strokes[0] };

        for (var i = 1; i < strokes.Count; i++)
        {
            var current = strokes[i];
            var previous = merged[^1];

            if (current.Direction == previous.Direction)
            {
                merged[^1] = previous with { Length = previous.Length + current.Length };
                continue;
            }

            merged.Add(current);
        }

        return merged;
    }

    private static List<DirectionStroke> RemoveNoise(IReadOnlyList<DirectionStroke> strokes)
    {
        if (strokes.Count == 0)
        {
            return [];
        }

        var filtered = new List<DirectionStroke>();

        for (var i = 0; i < strokes.Count; i++)
        {
            var current = strokes[i];
            var previous = i > 0 ? strokes[i - 1] : (DirectionStroke?)null;
            var next = i + 1 < strokes.Count ? strokes[i + 1] : (DirectionStroke?)null;

            if (current.Length < MinimumStrokeLength)
            {
                if (previous.HasValue && next.HasValue)
                {
                    if (previous.Value.Direction == next.Value.Direction)
                    {
                        continue;
                    }

                    if (IsOpposite(current.Direction, previous.Value.Direction) ||
                        IsOpposite(current.Direction, next.Value.Direction))
                    {
                        continue;
                    }
                }

                if (i == 0 || i == strokes.Count - 1)
                {
                    continue;
                }
            }

            if (previous.HasValue &&
                next.HasValue &&
                IsDiagonalBridge(previous.Value.Direction, current.Direction, next.Value.Direction) &&
                current.Length < Math.Min(previous.Value.Length, next.Value.Length) * 0.55)
            {
                continue;
            }

            if (previous.HasValue &&
                next.HasValue &&
                current.Length < Math.Min(previous.Value.Length, next.Value.Length) * 0.45 &&
                DirectionDistance(previous.Value.Direction, next.Value.Direction) <= 1)
            {
                continue;
            }

            filtered.Add(current);
        }

        return filtered;
    }

    private static GestureDirection ToDirection(int dx, int dy)
    {
        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        if (angle < 0)
        {
            angle += 360.0;
        }

        var sector = (int)Math.Round(angle / SectorSize) % 8;

        return sector switch
        {
            0 => GestureDirection.Right,
            1 => GestureDirection.DownRight,
            2 => GestureDirection.Down,
            3 => GestureDirection.DownLeft,
            4 => GestureDirection.Left,
            5 => GestureDirection.UpLeft,
            6 => GestureDirection.Up,
            7 => GestureDirection.UpRight,
            _ => GestureDirection.Right
        };
    }

    private static bool IsOpposite(GestureDirection a, GestureDirection b)
    {
        return DirectionDistance(a, b) == 4;
    }

    private static int DirectionDistance(GestureDirection a, GestureDirection b)
    {
        var diff = Math.Abs((int)a - (int)b);
        return Math.Min(diff, 8 - diff);
    }

    private static bool IsDiagonalBridge(
        GestureDirection previous,
        GestureDirection current,
        GestureDirection next)
    {
        return DirectionDistance(previous, next) == 2 &&
            DirectionDistance(previous, current) == 1 &&
            DirectionDistance(current, next) == 1;
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double PerpendicularDistance(Point point, Point lineStart, Point lineEnd)
    {
        var dx = lineEnd.X - lineStart.X;
        var dy = lineEnd.Y - lineStart.Y;

        if (dx == 0 && dy == 0)
        {
            return Distance(point, lineStart);
        }

        var numerator = Math.Abs(dy * point.X - dx * point.Y + lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X);
        var denominator = Math.Sqrt(dx * dx + dy * dy);
        return numerator / denominator;
    }

    private readonly record struct DirectionStroke(GestureDirection Direction, double Length);
}
