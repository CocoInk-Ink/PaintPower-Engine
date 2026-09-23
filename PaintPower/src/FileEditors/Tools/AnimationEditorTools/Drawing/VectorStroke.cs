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

    public void Render(Canvas canvas)
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

        var path = new Path
        {
            Data = geometry,
            Stroke = Brush,
            StrokeThickness = Thickness,
            StrokeJoin = PenLineJoin.Round,
            StrokeLineCap = PenLineCap.Round
        };


        canvas.Children.Add(path);
    }
}
