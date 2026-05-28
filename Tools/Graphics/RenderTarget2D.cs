using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PaintPower.Tools.Graphics;

public class RenderTarget2D
{
    public WriteableBitmap Bitmap { get; }
    public int Width => Bitmap.PixelSize.Width;
    public int Height => Bitmap.PixelSize.Height;
    public int Stride { get; private set; }

    public RenderTarget2D(int width, int height)
    {
        Bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
    }

    public unsafe void WithLock(Action<IntPtr, int, int, int> action)
    {
        using var fb = Bitmap.Lock();
        Stride = fb.RowBytes;
        action(fb.Address, Width, Height, Stride);
    }
}
