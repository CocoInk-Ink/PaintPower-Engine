using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PaintPower.EditorTools.PaintEditorTools;

public class Checkerboard : Control
{
    public static readonly StyledProperty<int> TileSizeProperty =
        AvaloniaProperty.Register<Checkerboard, int>(nameof(TileSize), 16);

    public static readonly StyledProperty<Color> LightColorProperty =
        AvaloniaProperty.Register<Checkerboard, Color>(nameof(LightColor), Colors.White);

    public static readonly StyledProperty<Color> DarkColorProperty =
        AvaloniaProperty.Register<Checkerboard, Color>(nameof(DarkColor), Colors.LightGray);

    public int TileSize
    {
        get => GetValue(TileSizeProperty);
        set => SetValue(TileSizeProperty, value);
    }

    public Color LightColor
    {
        get => GetValue(LightColorProperty);
        set => SetValue(LightColorProperty, value);
    }

    public Color DarkColor
    {
        get => GetValue(DarkColorProperty);
        set => SetValue(DarkColorProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        int size = TileSize;

        for (int y = 0; y < Bounds.Height; y += size)
        {
            for (int x = 0; x < Bounds.Width; x += size)
            {
                bool dark = (x / size + y / size) % 2 == 0;
                var brush = new SolidColorBrush(dark ? DarkColor : LightColor);
                context.FillRectangle(brush, new Rect(x, y, size, size));
            }
        }
    }
}
