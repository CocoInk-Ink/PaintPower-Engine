using System;

namespace PaintPower.Tools.Graphics;

public struct DrawCommand
{
    public Graphic Graphic;
    public float X;
    public float Y;
    public float Rotation;
    public float ScaleX;
    public float ScaleY;
    public int Z;

    public DrawCommand(Graphic g, float x, float y, float rot, float sx, float sy, int z)
    {
        Graphic = g;
        X = x;
        Y = y;
        Rotation = rot;
        ScaleX = sx;
        ScaleY = sy;
        Z = z;
    }
}
