using System;
using System.IO;
using System.IO.Compression;
using PaintPower.Logging;

namespace PaintPower.Sprites;

public class Skin
{
    public string? path = "";

    // Path to image.
    public Skin(string path)
    {
        this.path = path;
    }
}