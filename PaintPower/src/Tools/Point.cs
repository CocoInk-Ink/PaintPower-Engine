using System;

namespace PaintPower.Tools;

public class Point
{
    public double x = 0;
    public double y = 0;
    public Point(double? x, double? y)
    {
        if (x != null) this.x = (double)x;
        if (y != null) this.y = (double)y;
    }
}