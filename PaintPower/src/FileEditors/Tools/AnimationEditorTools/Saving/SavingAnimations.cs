using System;
using System.Collections.Generic;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools.Saving;

public class SavedAnimation
{
    public List<SavedFrame> Frames { get; set; } = new();
}

public class SavedFrame
{
    public int Index { get; set; }
    public List<SavedLayer> Layers { get; set; } = new();
}

public class SavedLayer
{
    public string Name { get; set; } = "";
    public bool Visible { get; set; }
    public bool Locked { get; set; }
    public List<SavedStroke> Strokes { get; set; } = new();
    public List<SavedShape> Shapes { get; set; } = new();
}

public class SavedStroke
{
    public double Thickness { get; set; }
    public string Color { get; set; } = "#000000";
    public double Opacity { get; set; }
    public List<SavedPoint> Points { get; set; } = new();
}

public class SavedPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class SavedShape
{
    public string Type { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Radius { get; set; }
    public string Color { get; set; } = "#FF0000";
}
