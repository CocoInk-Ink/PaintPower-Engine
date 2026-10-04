using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools.Drawing;

public class VectorStroke
{
    public List<Point> Points { get; } = new();
    public double Thickness { get; set; } = 2;
    public IBrush Brush { get; set; } = Brushes.Black;

    public void Render(Canvas canvas, double opacity)
    {
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            if (Points.Count > 0)
            {
                ctx.BeginFigure(Points[0], false);

                for (int i = 1; i < Points.Count; i++)
                    ctx.LineTo(Points[i]);
            }
        }

        // Brush is ALWAYS a SolidColorBrush because you create it that way.
        var solid = Brush as SolidColorBrush;

        // If something weird happens, fall back to black.
        if (solid == null)
            solid = new SolidColorBrush(Colors.Black, 1.0);

        // Multiply user opacity by onion opacity.
        double finalOpacity = solid.Opacity * opacity;

        var brush = new SolidColorBrush(solid.Color, finalOpacity);

        var path = new Path
        {
            Data = geometry,
            Stroke = brush,
            StrokeThickness = Thickness,
            StrokeJoin = PenLineJoin.Round,
            StrokeLineCap = PenLineCap.Round
        };

        canvas.Children.Add(path);
    }

    public bool EraseAlong(Point start, Point end, double radius, out List<VectorStroke> remaining)
    {
        remaining = new List<VectorStroke>();
        if (Points.Count == 0)
        {
            remaining.Add(this);
            return false;
        }

        double eraseRadius = Math.Max(0, radius) + Math.Max(0, Thickness) / 2;
        if (Points.Count == 1)
        {
            if (DistancePointToSegment(Points[0], start, end) <= eraseRadius)
                return true;

            remaining.Add(this);
            return false;
        }

        var fragments = new List<List<Point>>();
        List<Point>? current = null;
        bool erased = false;

        void Flush()
        {
            if (current is { Count: > 1 })
                fragments.Add(current);

            current = null;
        }

        void Append(Point a, Point b)
        {
            if (current == null || !current[^1].Equals(a))
            {
                Flush();
                current = new List<Point> { a };
            }

            current.Add(b);
        }

        for (int i = 0; i < Points.Count - 1; i++)
        {
            Point a = Points[i];
            Point b = Points[i + 1];

            if (!TryGetErasedInterval(a, b, start, end, eraseRadius, out double first, out double last))
            {
                Append(a, b);
                continue;
            }

            erased = true;

            if (first > 1e-9)
                Append(a, Interpolate(a, b, first));
            else
                Flush();

            Flush();

            if (last < 1 - 1e-9)
                Append(Interpolate(a, b, last), b);
        }

        Flush();

        if (!erased)
        {
            remaining.Add(this);
            return false;
        }

        foreach (var fragment in fragments)
        {
            var stroke = new VectorStroke
            {
                Thickness = Thickness,
                Brush = Brush
            };
            stroke.Points.AddRange(fragment);
            remaining.Add(stroke);
        }

        return true;
    }

    private static double DistancePointToSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;

        if (dx == 0 && dy == 0)
            return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));

        double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0, 1);

        double projX = a.X + t * dx;
        double projY = a.Y + t * dy;

        return Math.Sqrt(Math.Pow(p.X - projX, 2) + Math.Pow(p.Y - projY, 2));
    }

    private static bool TryGetErasedInterval(
        Point a,
        Point b,
        Point eraserStart,
        Point eraserEnd,
        double radius,
        out double first,
        out double last)
    {
        double DistanceAt(double t) => DistancePointToSegment(Interpolate(a, b, t), eraserStart, eraserEnd);

        double low = 0;
        double high = 1;
        for (int i = 0; i < 40; i++)
        {
            double third = (high - low) / 3;
            double firstThird = low + third;
            double secondThird = high - third;
            if (DistanceAt(firstThird) <= DistanceAt(secondThird))
                high = secondThird;
            else
                low = firstThird;
        }

        double minimum = (low + high) / 2;
        if (DistanceAt(minimum) > radius)
        {
            first = last = 0;
            return false;
        }

        if (DistanceAt(0) <= radius)
        {
            first = 0;
        }
        else
        {
            low = 0;
            high = minimum;
            for (int i = 0; i < 40; i++)
            {
                double middle = (low + high) / 2;
                if (DistanceAt(middle) <= radius)
                    high = middle;
                else
                    low = middle;
            }
            first = high;
        }

        if (DistanceAt(1) <= radius)
        {
            last = 1;
        }
        else
        {
            low = minimum;
            high = 1;
            for (int i = 0; i < 40; i++)
            {
                double middle = (low + high) / 2;
                if (DistanceAt(middle) <= radius)
                    low = middle;
                else
                    high = middle;
            }
            last = low;
        }

        return last - first > 1e-9;
    }

    private static Point Interpolate(Point a, Point b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
