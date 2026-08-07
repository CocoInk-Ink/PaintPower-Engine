using System;
using Toolbox.Graphics;

namespace Toolbox.Display.DisplayIntegration;

public class DIItem
{
    public double x;
    public double y;

    public double ScaleX = 1f;
    public double ScaleY = 1f;

    public bool IsVisible = true;

    public double Scale
    {
        get => ScaleX;
        set { ScaleX = value; ScaleY = value; }
    }

    public int Z = 0;

    public double Rotation = 90; // 90 is straight (right), 0 (up) 180 (down) -90 (left).
    public double StageWidth => 640;
    public double StageHeight => 450;

    public DIItem() { }

    // No longer abstract
    public virtual object? DrawAs()
    {
        return null;
    }

}