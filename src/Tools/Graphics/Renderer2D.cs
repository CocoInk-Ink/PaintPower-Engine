using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PaintPower.Tools.Graphics;

public class Renderer2D
{
    private readonly RenderTarget2D _target;

    public Renderer2D(RenderTarget2D target)
    {
        _target = target;
    }

    public unsafe void Clear(uint color)
    {
        _target.WithLock((ptr, w, h, stride) =>
        {
            Span<byte> span = new Span<byte>((void*)ptr, stride * h);
            for (int i = 0; i < span.Length; i += 4)
            {
                span[i + 0] = (byte)(color & 0xFF);         // B
                span[i + 1] = (byte)((color >> 8) & 0xFF);  // G
                span[i + 2] = (byte)((color >> 16) & 0xFF); // R
                span[i + 3] = (byte)((color >> 24) & 0xFF); // A
            }
        });
    }

    public unsafe void DrawPixel(int x, int y, uint color)
    {
        _target.WithLock((ptr, w, h, stride) =>
        {
            if (x < 0 || y < 0 || x >= w || y >= h)
                return;

            byte* pixels = (byte*)ptr;
            int index = y * stride + x * 4;

            pixels[index + 0] = (byte)(color & 0xFF);         // B
            pixels[index + 1] = (byte)((color >> 8) & 0xFF);  // G
            pixels[index + 2] = (byte)((color >> 16) & 0xFF); // R
            pixels[index + 3] = (byte)((color >> 24) & 0xFF); // A
        });
    }

    public unsafe void Blit(WriteableBitmap src, int dstX, int dstY)
    {
        _target.WithLock((dstPtr, dstW, dstH, dstStride) =>
        {
            using var srcFb = src.Lock();
            byte* srcPixels = (byte*)srcFb.Address;
            byte* dstPixels = (byte*)dstPtr;

            int srcW = src.PixelSize.Width;
            int srcH = src.PixelSize.Height;
            int srcStride = srcFb.RowBytes;

            for (int y = 0; y < srcH; y++)
            {
                int dy = dstY + y;
                if (dy < 0 || dy >= dstH) continue;

                for (int x = 0; x < srcW; x++)
                {
                    int dx = dstX + x;
                    if (dx < 0 || dx >= dstW) continue;

                    int srcIndex = y * srcStride + x * 4;
                    int dstIndex = dy * dstStride + dx * 4;

                    byte sb = srcPixels[srcIndex + 0];
                    byte sg = srcPixels[srcIndex + 1];
                    byte sr = srcPixels[srcIndex + 2];
                    byte sa = srcPixels[srcIndex + 3];

                    // Simple alpha blend over destination
                    byte db = dstPixels[dstIndex + 0];
                    byte dg = dstPixels[dstIndex + 1];
                    byte dr = dstPixels[dstIndex + 2];
                    byte da = dstPixels[dstIndex + 3];

                    float a = sa / 255f;
                    float ia = 1f - a;

                    dstPixels[dstIndex + 0] = (byte)(sb * a + db * ia);
                    dstPixels[dstIndex + 1] = (byte)(sg * a + dg * ia);
                    dstPixels[dstIndex + 2] = (byte)(sr * a + dr * ia);
                    dstPixels[dstIndex + 3] = 255;
                }
            }
        });
    }
}
