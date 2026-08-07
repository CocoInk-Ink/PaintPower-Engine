using Avalonia.Media.Imaging;
using System;
using System.Runtime.InteropServices;

namespace Toolbox.Graphics;

public static class BitmapExtensions
{
    public static byte[] CopyPixelsToArray(this WriteableBitmap bmp)
    {
        using var fb = bmp.Lock();
        byte[] arr = new byte[fb.RowBytes * bmp.PixelSize.Height];
        Marshal.Copy(fb.Address, arr, 0, arr.Length);
        return arr;
    }

    public static Avalonia.Media.Imaging.Bitmap ToAvaloniaBitmap(this Graphic g)
    {
        unsafe
        {
            fixed (byte* ptr = g.Pixels)
            {
                return new Avalonia.Media.Imaging.Bitmap(
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul,
                    (IntPtr)ptr,
                    new Avalonia.PixelSize(g.Width, g.Height),
                    new Avalonia.Vector(96, 96),
                    g.Width * 4
                );
            }
        }
    }
}
