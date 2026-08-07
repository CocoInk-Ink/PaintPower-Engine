// Skin.cs

using System;
using System.IO;
using System.IO.Compression;
using Toolbox.Logging;
using Toolbox.Graphics;

namespace Toolbox.Display.Sprites;

public class Skin
{
    public string Name;
    public string Path;
    public object Graphic; // Graphic or GraphicAnimation

    public Skin(string name, string path)
    {
        Name = name;
        Path = path;
        Graphic = GraphicLoader.LoadCached(path);
    }
}