/* Percent.cs */

using System;

namespace PaintPower.Tools.Math.Formulas;

public static class Percent
{
    public static int calc(double part, double total)
    {
        return (int)System.Math.Round((part / total) * 100);
    }
}