using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PaintPower.Tools.Graphics;

public class RenderTarget2D
{
    // The underlying pixel buffer
    public WriteableBitmap Bitmap { get; }

    // Width and height of the render target
    public int Width => Bitmap.PixelSize.Width;
    public int Height => Bitmap.PixelSize.Height;

    // Last known stride (updated during WithLock)
    public int Stride { get; private set; }

    public RenderTarget2D(int width, int height)
    {
        Bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
    }

    // Allows Renderer2D to write directly into the pixel buffer
    public unsafe void WithLock(Action<IntPtr, int, int, int> action)
    {
        using var fb = Bitmap.Lock();
        Stride = fb.RowBytes;
        action(fb.Address, Width, Height, Stride);
    }

    // Exposes the raw pixel pointer for DIPlay → Bitmap conversion
    public unsafe IntPtr BufferPtr
    {
        get
        {
            using var fb = Bitmap.Lock();
            return fb.Address;
        }
    }

    // Exposes the stride for DIPlay → Bitmap conversion
    public int CurrentStride
    {
        get
        {
            using var fb = Bitmap.Lock();
            return fb.RowBytes;
        }
    }
}
