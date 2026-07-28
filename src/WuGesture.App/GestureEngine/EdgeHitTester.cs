using System.Drawing;
using System.Windows.Forms;

namespace WuGesture.App.GestureEngine;

internal static class EdgeHitTester
{
    public static EdgeLocation GetCorner(Point location)
    {
        return GetCorner(location, Screen.AllScreens.Select(static screen => screen.Bounds));
    }

    public static EdgeLocation GetEdge(Point location)
    {
        return GetEdge(location, Screen.AllScreens.Select(static screen => screen.Bounds));
    }

    public static EdgeLocation GetFrictionEdge(Point location)
    {
        return GetFrictionEdge(location, Screen.AllScreens.Select(static screen => screen.Bounds));
    }

    public static int GetFrictionDistanceToEdge(EdgeLocation edge, Point location)
    {
        return GetFrictionDistanceToEdge(edge, location, Screen.AllScreens.Select(static screen => screen.Bounds));
    }

    internal static EdgeLocation GetCorner(Point location, IEnumerable<Rectangle> screenAreas)
    {
        foreach (var area in screenAreas)
        {
            var left = location.X <= area.Left + EdgeActionRuntimeDefaults.CornerSize;
            var right = location.X >= area.Right - EdgeActionRuntimeDefaults.CornerSize;
            var top = location.Y <= area.Top + EdgeActionRuntimeDefaults.CornerSize;
            var bottom = location.Y >= area.Bottom - EdgeActionRuntimeDefaults.CornerSize;

            if (left && top)
            {
                return EdgeLocation.TopLeft;
            }

            if (right && top)
            {
                return EdgeLocation.TopRight;
            }

            if (left && bottom)
            {
                return EdgeLocation.BottomLeft;
            }

            if (right && bottom)
            {
                return EdgeLocation.BottomRight;
            }
        }

        return EdgeLocation.None;
    }

    internal static EdgeLocation GetEdge(Point location, IEnumerable<Rectangle> screenAreas)
    {
        foreach (var area in screenAreas)
        {
            if (!ContainsInclusive(area, location))
            {
                continue;
            }

            if (location.X <= area.Left + EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Left;
            }

            if (location.X >= area.Right - EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Right;
            }

            if (location.Y <= area.Top + EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Top;
            }

            if (location.Y >= area.Bottom - EdgeActionRuntimeDefaults.EdgeThickness)
            {
                return EdgeLocation.Bottom;
            }
        }

        return EdgeLocation.None;
    }

    internal static EdgeLocation GetFrictionEdge(Point location, IEnumerable<Rectangle> screenAreas)
    {
        foreach (var area in screenAreas)
        {
            if (!ContainsInclusive(area, location))
            {
                continue;
            }

            var x = location.X - area.Left;
            var y = location.Y - area.Top;
            if (x <= EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                y > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                y < area.Height - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Left;
            }

            if (x >= area.Width - EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                y > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                y < area.Height - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Right;
            }

            if (y <= EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                x > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                x < area.Width - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Top;
            }

            if (y >= area.Height - EdgeActionRuntimeDefaults.FrictionEdgeThickness &&
                x > EdgeActionRuntimeDefaults.FrictionCornerExcludeSize &&
                x < area.Width - EdgeActionRuntimeDefaults.FrictionCornerExcludeSize)
            {
                return EdgeLocation.Bottom;
            }
        }

        return EdgeLocation.None;
    }

    internal static int GetFrictionDistanceToEdge(EdgeLocation edge, Point location, IEnumerable<Rectangle> screenAreas)
    {
        foreach (var area in screenAreas)
        {
            if (!ContainsInclusive(area, location))
            {
                continue;
            }

            return edge switch
            {
                EdgeLocation.Left => location.X - area.Left,
                EdgeLocation.Right => area.Right - location.X,
                EdgeLocation.Top => location.Y - area.Top,
                EdgeLocation.Bottom => area.Bottom - location.Y,
                _ => int.MaxValue
            };
        }

        return int.MaxValue;
    }

    private static bool ContainsInclusive(Rectangle area, Point location)
    {
        return location.X >= area.Left && location.X <= area.Right &&
            location.Y >= area.Top && location.Y <= area.Bottom;
    }
}
