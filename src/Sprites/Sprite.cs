using System;
using PaintPower.Logging;
using PaintPower.Display.Stage;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Tools.Graphics;

namespace PaintPower.Sprites;

public class Sprite : DIItem
{
    public Skin? skin;

    public Sprite(double? x = null, double? y = null, Skin? skin = null)
    {
        if (x != null) this.x = x; else this.x = Tools.Math.Random.calc(DIPlay.stageSize.x, DIPlay.stageSize.y);
        if (y != null) this.y = y;
        if (skin != null) this.skin = skin;
    }

    public void SetSkin(Skin skin)
    {
        this.skin = skin;
    }

    public override object DrawAs()
    {
        return Graphic.ToGraphic(skin);
    }

}