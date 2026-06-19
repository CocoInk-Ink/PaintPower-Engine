using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PaintPower.Tools.Graphics;

namespace PaintPower.ProjectSystem.SpriteEditor
{
    public class DrawingContextWrapper
    {
        private readonly Canvas _canvas;

        public DrawingContextWrapper(Canvas canvas)
        {
            _canvas = canvas;
            _canvas.Children.Clear();
        }

        public void Clear(Color color)
        {
            _canvas.Background = new SolidColorBrush(color);
        }

        public void DrawGraphic(Graphic g, double x, double y, double zoom)
        {
            var bmp = g.ToAvaloniaBitmap();
            var img = new Image
            {
                Source = bmp,
                Width = g.Width * zoom,
                Height = g.Height * zoom
            };

            Canvas.SetLeft(img, x);
            Canvas.SetTop(img, y);

            _canvas.Children.Add(img);
        }

        public void DrawRect(double x, double y, double w, double h, Color color, double thickness)
        {
            var rect = new Border
            {
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(thickness),
                Width = w,
                Height = h
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);

            _canvas.Children.Add(rect);
        }

        public void DrawString(string text, Brush brush, Point position)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = brush,
                Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
                Padding = new Thickness(4, 2)
            };

            Canvas.SetLeft(tb, position.X);
            Canvas.SetTop(tb, position.Y);

            _canvas.Children.Add(tb);
        }
    }
}