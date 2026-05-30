using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;

using Svg.Skia;
using SkiaSharp;

namespace PaintPower.Tools.Graphics;

public static class GraphicLoader
{
    private static readonly Dictionary<string, object> _cache = new();
    private static readonly object _lock = new();

    public static object LoadCached(string path)
    {
        path = Path.GetFullPath(path);

        lock (_lock)
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached;
        }

        object loaded = LoadByExtension(path);

        lock (_lock)
        {
            _cache[path] = loaded;
        }

        return loaded;
    }

    private static object LoadByExtension(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        return ext switch
        {
            ".gif"  => FromGIF(path),
            ".png"  => LoadRaster(path),
            ".jpg"  => LoadRaster(path),
            ".jpeg" => LoadRaster(path),
            ".bmp"  => LoadRaster(path),
            ".webp" => LoadRaster(path),
            ".tiff" => LoadRaster(path),
            ".svg" => LoadVectorImage(path, "svg", "xml"),
            _ => throw new NotSupportedException("Unsupported image format: " + ext)
        };
    }

    // Path, type, Markup Language.
    private static Graphic LoadVectorImage(string path, string type, string ML)
    {
        if (ML == "xml")
        {
            return type switch
            {
                "svg" => LoadSVGImage(path),
                _ => LoadSVGImage(path)
            };
        }

        return new Graphic(0, 0, new byte[268]);
    }

private static Graphic LoadSVGImage(string path)
{
    // Load SVG
    var svg = new SKSvg();
    svg.Load(path);

    if (svg.Picture == null)
        throw new Exception("Failed to load SVG: " + path);

    // Determine output size
    int width = (int)svg.Picture.CullRect.Width;
    int height = (int)svg.Picture.CullRect.Height;

    if (width <= 0 || height <= 0)
        throw new Exception("SVG has invalid dimensions: " + path);

    // Render SVG to Skia bitmap
    using var skBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var canvas = new SKCanvas(skBitmap);

    canvas.Clear(SKColors.Transparent);
    canvas.DrawPicture(svg.Picture);
    canvas.Flush();

    // Convert Skia bitmap → ImageSharp image
    using var img = Image.LoadPixelData<Rgba32>(skBitmap.Bytes, width, height);

    // Extract pixel data
    byte[] pixels = new byte[width * height * 4];
    img.CopyPixelDataTo(pixels);

    // Convert RGBA → BGRA for your renderer
    ConvertRgbaToBgra(pixels);

    return new Graphic(width, height, pixels);
}

    public static Graphic LoadRaster(string path)
    {
        using Image<Rgba32> img = Image.Load<Rgba32>(path);

        int width = img.Width;
        int height = img.Height;

        byte[] pixels = new byte[width * height * 4];
        img.CopyPixelDataTo(pixels);

        ConvertRgbaToBgra(pixels);

        return new Graphic(width, height, pixels);
    }

    public static GraphicAnimation FromGIF(string path)
    {
        using Image<Rgba32> gif = Image.Load<Rgba32>(path);

        var anim = new GraphicAnimation();

        foreach (var frame in gif.Frames)
        {
            int width = frame.Width;
            int height = frame.Height;

            byte[] pixels = new byte[width * height * 4];
            frame.CopyPixelDataTo(pixels);

            ConvertRgbaToBgra(pixels);

            anim.Frames.Add(new Graphic(width, height, pixels));

            var meta = frame.Metadata.GetGifMetadata();
            int delay = meta.FrameDelay * 10;
            anim.FrameDelays.Add(delay == 0 ? 100 : delay);
        }

        return anim;
    }

    private static void ConvertRgbaToBgra(byte[] pixels)
    {
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte r = pixels[i + 0];
            byte g = pixels[i + 1];
            byte b = pixels[i + 2];
            byte a = pixels[i + 3];

            pixels[i + 0] = b;
            pixels[i + 1] = g;
            pixels[i + 2] = r;
            pixels[i + 3] = a;
        }
    }
}
