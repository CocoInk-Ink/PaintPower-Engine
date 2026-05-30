using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PaintPower.Tools;
using PaintPower.Tools.Graphics;
using Point = PaintPower.Tools.Point;

namespace PaintPower.Display.DisplayIntegration;

// DIPlay or Diplay. You know, like Dip-lay, like "Display" with out the 's'. (Inspired by a typo while learning CSS)
public class DIPlay
{
    // The off-screen rendering surface (your custom pixel renderer)
    public GfxPane gfxPane;

    // Stage size based on project metadata
    public static Point stageSize = new(
        PaintPower_Engine.App._project?.Metadata?.StageWidth,
        PaintPower_Engine.App._project?.Metadata?.StageHeight
    );

    // All renderable items (sprites, UI elements, etc.)
    public List<DIItem> items = new();

    // Reference to the Stage UserControl so we can push rendered frames into it
    private readonly VMPanel.Stage _stage;

    // Animation timer (must persist across frames)
    private int _t = 0;

    public DIPlay(int width, int height, VMPanel.Stage stage)
    {
        _stage = stage;

        // Create the pixel buffer with the given size
        gfxPane = new GfxPane(width, height);
    }

    // Starts the 60 FPS render loop
    public void Start()
    {
        var timer = new System.Timers.Timer(1000.0 / 60.0);
        timer.Elapsed += async (_, __) => Tick();
        timer.Start();
    }

    // Main render loop — runs every frame
    public async void Tick()
    {
        var pane = gfxPane;

        if (PaintPower_Engine.App.vm != null)
        {

            // Update VM logic before rendering
            await PaintPower_Engine.App.vm.Tick();

        }

        // Build a list of draw commands for batching
        List<DrawCommand> batch = new();

        foreach (DIItem item in items)
        {
            // Skip invisible items
            if (!item.IsVisible)
                continue;

            // Ask the item what graphic it wants to draw
            var g = item.DrawAs();

            // Handle animated graphics
            if (g is GraphicAnimation anim)
            {
                // Pick the correct animation frame based on time
                int frame = ResolveAnimationFrame(anim, _t * 16);
                Graphic frameGraphic = anim.Frames[frame];

                // Skip if off-screen
                if (IsCulled(frameGraphic, (float)item.x, (float)item.y, item.ScaleX, item.ScaleY))
                    continue;

                // Add to batch
                batch.Add(new DrawCommand(
                    frameGraphic,
                    (float)item.x,
                    (float)item.y,
                    item.Rotation,
                    item.ScaleX,
                    item.ScaleY,
                    item.Z
                ));
            }
            // Handle static images
            else if (g is Graphic img)
            {
                if (IsCulled(img, (float)item.x, (float)item.y, item.ScaleX, item.ScaleY))
                    continue;

                batch.Add(new DrawCommand(
                    img,
                    (float)item.x,
                    (float)item.y,
                    item.Rotation,
                    item.ScaleX,
                    item.ScaleY,
                    item.Z
                ));
            }
        }

        // Sort by Z so items draw in the correct order
        batch.Sort((a, b) => a.Z.CompareTo(b.Z));

        // Clear the frame buffer
        pane.Renderer.Clear(0xFF202020);

        // Draw all items in one batch
        pane.Renderer.DrawBatch(batch);

        // Push the rendered bitmap into the Stage UI
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var bmp = CreateBitmapFromPane();
            _stage.SetBitmap(bmp);
        });

        // Advance animation timer
        _t++;
    }

    // Determines whether a sprite is off-screen and can be skipped
    private bool IsCulled(Graphic g, float x, float y, float scaleX, float scaleY)
    {
        float halfW = g.Width * scaleX / 2f;
        float halfH = g.Height * scaleY / 2f;

        float left = x - halfW;
        float right = x + halfW;
        float top = y - halfH;
        float bottom = y + halfH;

        return right < 0 ||
               left > stageSize.x ||
               bottom < 0 ||
               top > stageSize.y;
    }

    // Chooses the correct animation frame based on time
    private int ResolveAnimationFrame(GraphicAnimation anim, int timeMs)
    {
        int total = 0;

        for (int i = 0; i < anim.FrameDelays.Count; i++)
        {
            total += anim.FrameDelays[i];
            if (timeMs % total < anim.FrameDelays[i])
                return i;
        }

        return 0;
    }

    // Converts the raw BGRA pixel buffer into an Avalonia Bitmap
    private Bitmap CreateBitmapFromPane()
    {
        return new Bitmap(
            PixelFormat.Bgra8888,
            AlphaFormat.Premul,
            gfxPane.BufferPtr,   // pointer to raw BGRA data
            new PixelSize((int)gfxPane.Width, (int)gfxPane.Height),
            new Vector(96, 96),  // DPI
            gfxPane.Stride
        );
    }
}
