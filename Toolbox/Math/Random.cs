using System;

namespace Toolbox.Math;

public static class Random
{
    public static double calc(double min, double max)
    {
        System.Random random = new();
        return random.NextDouble() * (max - min) + min;
    }
}