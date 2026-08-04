using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace WuGesture.App;

/// <summary>
/// Draws a fading overlay into a premultiplied-alpha surface before compositing it
/// onto the shared layered-window buffer. This keeps text antialiasing independent
/// from the transparent black backing buffer.
/// </summary>
internal sealed class FadeOverlaySurface : IDisposable
{
    private Bitmap? bitmap;

    public void Draw(Graphics destination, Rectangle bounds, byte opacity, Action<Graphics> drawContent)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || opacity == 0)
        {
            return;
        }

        EnsureBitmap(bounds.Size);

        using (var surfaceGraphics = Graphics.FromImage(bitmap!))
        {
            surfaceGraphics.Clear(Color.Transparent);
            surfaceGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            surfaceGraphics.CompositingMode = CompositingMode.SourceOver;
            surfaceGraphics.CompositingQuality = CompositingQuality.HighQuality;
            surfaceGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            surfaceGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            surfaceGraphics.TranslateTransform(-bounds.Left, -bounds.Top);
            drawContent(surfaceGraphics);
        }

        if (opacity == byte.MaxValue)
        {
            destination.DrawImage(bitmap!, bounds);
            return;
        }

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity / 255f };
        attributes.SetColorMatrix(matrix);
        destination.DrawImage(
            bitmap!,
            bounds,
            0,
            0,
            bitmap!.Width,
            bitmap.Height,
            GraphicsUnit.Pixel,
            attributes);
    }

    public void Dispose()
    {
        bitmap?.Dispose();
    }

    private void EnsureBitmap(Size size)
    {
        if (bitmap?.Size == size)
        {
            return;
        }

        bitmap?.Dispose();
        bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
    }
}
