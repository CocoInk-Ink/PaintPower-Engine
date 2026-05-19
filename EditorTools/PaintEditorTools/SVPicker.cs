using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace PaintPower.EditorTools.PaintEditorTools;

public class SVPicker : Control
{
    public static readonly StyledProperty<double> HueProperty =
        AvaloniaProperty.Register<SVPicker, double>(nameof(Hue));

    public static readonly StyledProperty<double> SaturationProperty =
        AvaloniaProperty.Register<SVPicker, double>(nameof(Saturation));

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SVPicker, double>(nameof(Value));

    public double Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public double Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private bool _isDragging;

    public SVPicker()
    {
        PointerPressed += (s, e) =>
        {
            _isDragging = true;
            UpdateFromPointer(e);
            e.Pointer.Capture(this);
        };

        PointerReleased += (s, e) =>
        {
            _isDragging = false;
            e.Pointer.Capture(null);
        };

        PointerMoved += (s, e) =>
        {
            if (_isDragging)
                UpdateFromPointer(e);
        };

        // Redraw when Hue changes
        PropertyChanged += (_, e) =>
        {
            if (e.Property == HueProperty)
                InvalidateVisual();
        };
    }

    private void UpdateFromPointer(PointerEventArgs e)
    {
        var p = e.GetPosition(this);

        Saturation = Math.Clamp(p.X / Bounds.Width, 0, 1);
        Value = 1 - Math.Clamp(p.Y / Bounds.Height, 0, 1);

        InvalidateVisual();
    }

    public override void Render(DrawingContext ctx)
    {
        var rect = new Rect(Bounds.Size);

        // Horizontal gradient: white → hue color
        var hueColor = ColorFromHSV(Hue, 1, 1);

        var satBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.White, 0),
                new GradientStop(hueColor, 1)
            }
        };

        ctx.FillRectangle(satBrush, rect);

        // Vertical gradient: transparent → black
        var valBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(Colors.Black, 1)
            }
        };

        ctx.FillRectangle(valBrush, rect);
    }

    public static Color ColorFromHSV(double h, double s, double v)
    {
        h = h % 360;
        double c = v * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; }
        else if (h < 120) { r = x; g = c; }
        else if (h < 180) { g = c; b = x; }
        else if (h < 240) { g = x; b = c; }
        else if (h < 300) { r = x; b = c; }
        else { r = c; b = x; }

        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255)
        );
    }
}