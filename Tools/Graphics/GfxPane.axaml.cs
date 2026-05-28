using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Threading;

namespace PaintPower.Tools.Graphics;

public partial class GfxPane : Control
{
    private readonly RenderTarget2D _target;
    private readonly Renderer2D _renderer;

    public Renderer2D Renderer => _renderer;

    public GfxPane(int width, int height)
    {
        _target = new RenderTarget2D(width, height);
        _renderer = new Renderer2D(_target);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.DrawImage(
            _target.Bitmap,
            new Rect(0, 0, _target.Width, _target.Height),
            new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    public void Present()
    {
        InvalidateVisual();
    }
}
