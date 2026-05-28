using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Document;
using System.Collections.Generic;

public class RainbowBlockRenderer : IBackgroundRenderer
{
    private readonly TextEditor _editor;

    private static readonly Color[] BlockColors =
    {
        Color.Parse("#FFD700"), // Yellow
        Color.Parse("#C586C0"), // Purple
        Color.Parse("#569CD6"), // Blue
        Color.Parse("#4EC9B0"), // Teal
        Color.Parse("#D16969"), // Red
    };

    public KnownLayer Layer => KnownLayer.Background;

    public RainbowBlockRenderer(TextEditor editor)
    {
        _editor = editor;
    }

    private static Color WithAlpha(Color c, double alpha)
    {
        byte a = (byte)(alpha * 255);
        return new Color(a, c.R, c.G, c.B);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_editor.Document == null)
            return;

        textView.EnsureVisualLines();

        string text = _editor.Document.Text;
        var stack = new Stack<int>();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '{')
            {
                stack.Push(i);
            }
            else if (c == '}' && stack.Count > 0)
            {
                int start = stack.Pop();
                int end = i;

                int depth = stack.Count % BlockColors.Length;
                var color = WithAlpha(BlockColors[depth], 0.08);

                DrawBlock(textView, drawingContext, start, end, color);
            }
        }
    }

    private void DrawBlock(TextView view, DrawingContext ctx, int startOffset, int endOffset, Color color)
    {
        var startLoc = _editor.Document.GetLocation(startOffset);
        var endLoc = _editor.Document.GetLocation(endOffset);

        var startPos = view.GetVisualPosition(
            new TextViewPosition(startLoc),
            VisualYPosition.LineTop
        );

        var endPos = view.GetVisualPosition(
            new TextViewPosition(endLoc),
            VisualYPosition.LineBottom
        );

        var rect = new Rect(
            new Point(0, startPos.Y),
            new Point(view.Bounds.Width, endPos.Y)
        );

        ctx.FillRectangle(new SolidColorBrush(color), rect);
    }
}