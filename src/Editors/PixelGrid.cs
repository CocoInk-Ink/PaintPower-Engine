using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PaintPower.Editors;

public class PixelGrid : Control
{
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<PixelGrid, double>(nameof(Zoom));

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public static readonly StyledProperty<int> PixelWidthProperty =
        AvaloniaProperty.Register<PixelGrid, int>(nameof(PixelWidth));

    public int PixelWidth
    {
        get => GetValue(PixelWidthProperty);
        set => SetValue(PixelWidthProperty, value);
    }

    public static readonly StyledProperty<int> PixelHeightProperty =
        AvaloniaProperty.Register<PixelGrid, int>(nameof(PixelHeight));

    public int PixelHeight
    {
        get => GetValue(PixelHeightProperty);
        set => SetValue(PixelHeightProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Zoom < 5)
            return; // Only show grid when zoomed in enough

        double step = Zoom; // 1 pixel = Zoom units

        var minorPen = new Pen(Brushes.Gray, 1);
        var majorPen = new Pen(Brushes.White, 1); // thicker, brighter

        // Vertical lines
        for (int x = 0; x <= PixelWidth; x++)
        {
            double px = x * step;
            context.DrawLine(minorPen, new Point(px, 0), new Point(px, PixelHeight * step));
        }

        // Horizontal lines
        for (int y = 0; y <= PixelHeight; y++)
        {
            double py = y * step;

            context.DrawLine(minorPen, new Point(0, py), new Point(PixelWidth * step, py));
        }
    }
}