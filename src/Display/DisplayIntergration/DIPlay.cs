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
                double ix = item.x ?? 0;
                double iy = item.y ?? 0;

                // Snapshot-based sprites
                if (item is Sprite sprite)
                {
                    if (sprite.SnapshotDirty)
                        sprite.RenderSnapshot();

                    var g = sprite.SnapshotGraphic;
                    if (g == null)
                        continue;

                    if (IsCulled(g, (float)ix, (float)iy, sprite.ScaleX, sprite.ScaleY))
                        continue;

                    batch.Add(new DrawCommand(
                        g,
                        (float)ix,
                        (float)iy,
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
                    if (IsCulled(graphic, (float)ix, (float)iy, item.ScaleX, item.ScaleY))
                        continue;

                    batch.Add(new DrawCommand(
                        graphic,
                        (float)ix,
                        (float)iy,
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

        private bool IsCulled(Graphic g, float x, float y, float scaleX, float scaleY)
        {
            float halfW = g.Width * scaleX / 2f;
            float halfH = g.Height * scaleY / 2f;

            float left = x - halfW;
            float right = x + halfW;
            float top = y - halfH;
            float bottom = y + halfH;

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
