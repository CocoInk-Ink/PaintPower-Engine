using System;
using System.Collections.Generic;
using PaintPower.Tools;
using PaintPower.Tools.Graphics;

namespace PaintPower.Display.DisplayIntegration;

// DIPlay or Diplay. You know, like Dip-lay, like "Display" with out the 's'. (Inspired by a typo while learning CSS)
public class DIPlay
{
    public GfxPane gfxPane;
    public static Point stageSize = new(PaintPower_Engine.App._project?.Metadata?.StageWidth, PaintPower_Engine.App._project?.Metadata?.StageHeight);

    public List<DIItem> items = new();

    public DIPlay()
    {
        setStageSize();
        gfxPane = new(stageSize.x, stageSize.y);
    }

    public void setStageSize()
    {
        stageSize = new(PaintPower_Engine.App._project?.Metadata?.StageWidth, PaintPower_Engine.App._project?.Metadata?.StageHeight);
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
        int t = 0;

        await PaintPower_Engine.App.vm.Tick();

        List<DrawCommand> batch = new();

        foreach (DIItem item in items)
        {
            if (!item.IsVisible)
                continue;

            var g = item.DrawAs();

            if (g is GraphicAnimation anim)
            {
                int frame = ResolveAnimationFrame(anim, t * 16);
                Graphic frameGraphic = anim.Frames[frame];

                if (IsCulled(frameGraphic, (float)item.x, (float)item.y, item.ScaleX, item.ScaleY))
                    continue;

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

        // Sort for Z-Layering
        batch.Sort((a, b) => a.Z.CompareTo(b.Z));

        pane.Renderer.Clear(0xFF202020);
        pane.Renderer.DrawBatch(batch);

        Avalonia.Threading.Dispatcher.UIThread.Post(pane.Present);

        t++;
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
               left > stageSize.x ||
               bottom < 0 ||
               top > stageSize.y;
    }

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
}