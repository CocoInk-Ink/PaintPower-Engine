using System;
using Avalonia.Controls;

namespace PaintPower.Tools.Graphics;

public partial class TestGfx : UserControl
{
    public TestGfx()
    {

    }

    public async void Test()
    {
        var pane = new GfxPane(640, 360);
        int t = 0;

        var timer = new System.Timers.Timer(1000.0 / 60.0);
        timer.Elapsed += (_, __) =>
        {
            int x = (int)(320 + System.Math.Sin(t / 30.0) * 100);
            int y = 180;

            pane.Renderer.Clear(0xFF202020); // ARGB
            pane.Renderer.DrawPixel(x, y, 0xFFFFCC00);

            Avalonia.Threading.Dispatcher.UIThread.Post(pane.Present);

            t++;
        };
        timer.Start();
    }
}