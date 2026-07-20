using System.Drawing.Drawing2D;
using WuGesture.App.GestureEngine;

namespace WuGesture.App;

internal sealed class MouseTrailRenderer : IDisposable
{
    private Pen inactivePen;
    private Pen activePen;
    private Pen dirtyPen;
    private readonly GraphicsPath path = new();
    private readonly GraphicsPath dirtyPath = new();
    private PointF lastPoint;
    private bool hasLastPoint;
    private bool isHighlighted;

    public MouseTrailRenderer(float dpiFactor)
    {
        var pathWidth = 3f * dpiFactor;
        inactivePen = CreatePen(Color.FromArgb(255, 170, 170, 170), pathWidth);
        activePen = CreatePen(Color.SkyBlue, pathWidth);
        dirtyPen = CreatePen(Color.White, pathWidth * 3.5f);
    }

    public bool HasPath => path.PointCount > 0;

    public void ApplySettings(MouseTrailUiSettings? settings, float dpiFactor)
    {
        var uiSettings = settings ?? new MouseTrailUiSettings();
        var inactivePathWidth = Math.Max(1f, uiSettings.InactiveThickness) * dpiFactor;
        var activePathWidth = Math.Max(1f, uiSettings.ActiveThickness) * dpiFactor;
        var inactiveColor = ApplyOpacity(
            GestureColorParser.Parse(uiSettings.InactiveColor, Color.FromArgb(255, 170, 170, 170)),
            ClampOpacity(uiSettings.InactiveOpacity));
        var activeColor = ApplyOpacity(
            GestureColorParser.Parse(uiSettings.ActiveColor, Color.SkyBlue),
            ClampOpacity(uiSettings.ActiveOpacity));

        inactivePen.Dispose();
        activePen.Dispose();
        dirtyPen.Dispose();

        inactivePen = CreatePen(inactiveColor, inactivePathWidth);
        activePen = CreatePen(activeColor, activePathWidth);
        dirtyPen = CreatePen(Color.White, Math.Max(inactivePathWidth, activePathWidth) * 3.5f);
    }

    public bool SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return false;
        }

        isHighlighted = highlighted;
        return true;
    }

    public void StartPath()
    {
        path.Reset();
        dirtyPath.Reset();
        hasLastPoint = false;
    }

    public Rectangle? AppendPoint(Graphics graphics, PointF point, Size bounds)
    {
        if (!hasLastPoint)
        {
            hasLastPoint = true;
            lastPoint = point;
            return new Rectangle((int)point.X, (int)point.Y, 1, 1);
        }

        if (DistanceSquared(lastPoint, point) < 0.25f)
        {
            return null;
        }

        path.AddLine(lastPoint, point);
        graphics.DrawLine(isHighlighted ? activePen : inactivePen, lastPoint, point);

        dirtyPath.Reset();
        dirtyPath.AddLine(lastPoint, point);
        dirtyPath.Widen(dirtyPen);
        lastPoint = point;

        var dirtyRect = Rectangle.Ceiling(dirtyPath.GetBounds());
        dirtyRect.Intersect(new Rectangle(Point.Empty, bounds));
        return dirtyRect;
    }

    public void Draw(Graphics graphics)
    {
        if (path.PointCount > 0)
        {
            graphics.DrawPath(isHighlighted ? activePen : inactivePen, path);
        }
    }

    public void Reset()
    {
        hasLastPoint = false;
        isHighlighted = false;
        path.Reset();
        dirtyPath.Reset();
    }

    public void Dispose()
    {
        inactivePen.Dispose();
        activePen.Dispose();
        dirtyPen.Dispose();
        path.Dispose();
        dirtyPath.Dispose();
    }

    private static Color ApplyOpacity(Color color, byte opacity)
    {
        return Color.FromArgb(opacity, color.R, color.G, color.B);
    }

    private static byte ClampOpacity(int value)
    {
        var scaled = (int)Math.Round(value * 255d / 100d);
        return (byte)Math.Max(0, Math.Min(255, scaled));
    }

    private static float DistanceSquared(PointF a, PointF b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static Pen CreatePen(Color color, float width)
    {
        return new Pen(color, width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
    }
}
