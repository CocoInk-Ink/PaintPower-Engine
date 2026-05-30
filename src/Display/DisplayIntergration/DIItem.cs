using System;
using PaintPower.Tools.Graphics;

namespace PaintPower.Display.DisplayIntegration;

public abstract class DIItem
{
    public double? x;
    public double? y;

    public float ScaleX = 1f;
    public float ScaleY = 1f;

    public bool IsVisible = true;

    public float Scale
    {
        get => ScaleX;
        set { ScaleX = value; ScaleY = value; }
    }

    public int Z = 0;

    public float Rotation = 0f; // 90 is normal, goes from 0 - 360.

    public DIItem() { }

    public abstract object DrawAs();

}