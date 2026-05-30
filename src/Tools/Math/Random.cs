using System;
using PaintPower.Templates.FileTemplates;

namespace PaintPower.Tools.Math;

public static class Random
{
    public static double calc(double min, double max)
    {
        System.Random random = new();
        return random.NextDouble() * (max - min) + min;
    }
}