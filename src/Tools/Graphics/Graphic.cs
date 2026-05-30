using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;

using PaintPower.Sprites;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PaintPower.Tools.Graphics;

public class Graphic
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; } // BGRA pixel data

    public Graphic(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    // Convert a Skin (file path) into a Graphic
    public static object ToGraphic(Skin? skin)
    {
        if (skin == null || string.IsNullOrWhiteSpace(skin.path))
            throw new ArgumentException("Skin is null or has no path.");

        return GraphicLoader.LoadCached(skin.path);
    }

}

public class GraphicAnimation
{
    public List<Graphic> Frames { get; } = new();
    public List<int> FrameDelays { get; } = new();
}
