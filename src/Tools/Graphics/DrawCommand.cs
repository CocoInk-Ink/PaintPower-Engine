using System;

namespace PaintPower.Tools.Graphics;

public struct DrawCommand
{
    public Graphic Graphic;
    public double X;
    public double Y;
    public double Rotation;
    public double ScaleX;
    public double ScaleY;
    public int Z;

    public DrawCommand(Graphic g, double x, double y, double rot, double sx, double sy, int z)
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
