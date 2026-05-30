using System;
using PaintPower.Logging;
using PaintPower.Display.Stage;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Tools.Graphics;
using System.Collections.Generic;

namespace PaintPower.Sprites;

public class Sprite : DIItem
{
    public List<Skin> Skins = new();
    public int CurrentSkinIndex = 0;

    public Skin CurrentSkin => Skins[CurrentSkinIndex];

    public Sprite(double? x = null, double? y = null, int? skin = null)
    {
        if (x != null) this.x = x; else this.x = Tools.Math.Random.calc(DIPlay.stageSize.x, DIPlay.stageSize.y);
        if (y != null) this.y = y;
        if (skin != null) this.CurrentSkinIndex = (int)skin;
    }

    public void SetSkin(int index)
    {
        if (index >= 0 && index < Skins.Count)
            CurrentSkinIndex = index;
    }

    public override object DrawAs()
    {
        if (Skins.Count == 0)
            return null; // DIPlay will skip it
        return CurrentSkin.Graphic;
    }

}