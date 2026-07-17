using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PaintPower.Tools;
using PaintPower.Tools.Graphics;
using PaintPower.Vm.Runtime.Sprites;
using Point = PaintPower.Tools.Point;

namespace PaintPower.Display.DisplayIntegration
{
    // DIPlay or Diplay. You know, like Dip-lay, like "Display" with out the 's'. (Inspired by a typo while learning CSS)
    public class DIPlay
    {
        public GfxPane gfxPane;

        public Point StageSize { get; set; }

        public List<DIItem> items = new();

        private readonly VMPanel.Stage _stage;

        private int _t = 0;

        public DIPlay(int width, int height, VMPanel.Stage stage, double stageWidth, double stageHeight)
        {
            _stage = stage;
            gfxPane = new GfxPane(width, height);
            StageSize = new Point(stageWidth, stageHeight);
        }

        public void Start()
        {
            var timer = new System.Timers.Timer(1000.0 / 60.0);
            timer.Elapsed += async (_, __) => Tick();
            timer.Start();
        }

        public async void Tick()
        {
            var pane = gfxPane;

            List<DrawCommand> batch = new();

            foreach (DIItem item in items)
            {
                if (!item.IsVisible)
                    continue;

                // Resolve safe position (old projects may have null x/y)
                double ix = item.x;
                double iy = item.y;

                // Snapshot-based sprites
                if (item is Sprite sprite)
                {
                    if (sprite.SnapshotDirty)
                        sprite.RenderSnapshot();

                    var g = sprite.SnapshotGraphic;
                    if (g == null)
                        continue;

                    if (IsCulled(g, (double)ix, (double)iy, sprite.ScaleX, sprite.ScaleY))
                        continue;

                    double drawX = ix - (g.Width * sprite.ScaleX) / 2.0;
                    double drawY = iy - (g.Height * sprite.ScaleY) / 2.0;

                    batch.Add(new DrawCommand(
                        g,
                        drawX,
                        drawY,
                        sprite.Rotation,
                        sprite.ScaleX,
                        sprite.ScaleY,
                        sprite.Z
                    ));

                    continue;
                }

                // Other DIItems that return a Graphic directly
                var graphic = item.DrawAs() as Graphic;
                if (graphic != null)
                {
                    if (IsCulled(graphic, ix, iy, item.ScaleX, item.ScaleY))
                        continue;

                    double drawX = ix - (graphic.Width * item.ScaleX) / 2.0;
                    double drawY = iy - (graphic.Height * item.ScaleY) / 2.0;

                    batch.Add(new DrawCommand(
                        graphic,
                        drawX,
                        drawY,
                        item.Rotation,
                        item.ScaleX,
                        item.ScaleY,
                        item.Z
                    ));
                }
            }

            batch.Sort((a, b) => a.Z.CompareTo(b.Z));

            pane.Renderer.Clear(0xFF202020);
            pane.Renderer.DrawBatch(batch);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _stage.SetBitmap(CreateBitmapFromPane());
            });

            _t++;
        }

        private bool IsCulled(Graphic g, double x, double y, double scaleX, double scaleY)
        {
            double halfW = g.Width * scaleX / 2f;
            double halfH = g.Height * scaleY / 2f;

            double left = x - halfW;
            double right = x + halfW;
            double top = y - halfH;
            double bottom = y + halfH;

            return right < 0 ||
                left > StageSize.x ||
                bottom < 0 ||
                top > StageSize.y;

        }

        private Bitmap CreateBitmapFromPane()
        {
            return new Bitmap(
                PixelFormat.Bgra8888,
                AlphaFormat.Premul,
                gfxPane.BufferPtr,
                new PixelSize((int)gfxPane.Width, (int)gfxPane.Height),
                new Vector(96, 96),
                gfxPane.Stride
            );
        }
    }
}
