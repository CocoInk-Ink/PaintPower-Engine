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

}

public class GraphicAnimation
{
    public List<Graphic> Frames { get; } = new();
    public List<int> FrameDelays { get; } = new();
}
