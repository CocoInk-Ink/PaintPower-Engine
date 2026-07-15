using System;
using System.Linq;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Tools.Graphics;
using System.Collections.Generic;

namespace PaintPower.Vm.Runtime.Sprites
{
    public class Sprite : DIItem
    {


        // All skins (loaded from Skins.xml)
        public List<RuntimeSkin> Skins = new();
        public int CurrentSkinIndex = 0;

        // Snapshot graphic (what DIPlay draws)
        public Graphic SnapshotGraphic { get; private set; }

        // Whether the snapshot needs to be rebuilt
        public bool SnapshotDirty { get; set; } = true;

        public RuntimeSkin? CurrentSkin
        {
            get
            {
                if (Skins.Count == 0)
                    return null;

                if (CurrentSkinIndex < 0 || CurrentSkinIndex >= Skins.Count)
                    CurrentSkinIndex = 0;

                return Skins[CurrentSkinIndex];
            }
        }

        public Sprite(double? x = null, double? y = null, int? skin = null)
        {
            if (x != null)
                this.x = x;
            else
                this.x = Tools.Math.Random.calc(StageWidth, StageHeight);

            if (y != null)
                this.y = y;

            if (skin != null)
                this.CurrentSkinIndex = (int)skin;
        }

        public void SetSkin(int index)
        {
            if (index >= 0 && index < Skins.Count)
            {
                CurrentSkinIndex = index;
                SnapshotDirty = true;
            }
        }


        // ---------------------------------------------------------
        // Snapshot Rendering
        // ---------------------------------------------------------
        public void RenderSnapshot()
        {
            var skin = CurrentSkin;
            if (skin == null)
                return;

            int w = (int)StageWidth;
            int h = (int)StageHeight;

            // Create a temporary render target
            var target = new RenderTarget2D(w, h);
            var renderer = new Renderer2D(target);

            // Clear transparent
            renderer.Clear(0x00000000);

            // Draw each element in Z order
            foreach (var elem in skin.Elements.OrderBy(e => e.ZIndex))
            {
                double finalX = elem.X + w / 2;
                double finalY = elem.Y + h / 2;

                if (elem is RuntimeImageElement img)
                {
                    if (img.Graphic != null)
                    {
                        renderer.DrawGraphic(
                            img.Graphic,
                            (int)finalX,
                            (int)finalY,
                            (float)elem.Rotation,
                            (float)elem.ScaleX,
                            (float)elem.ScaleY
                        );
                    }
                }
                else if (elem is RuntimeVideoElement vid)
                {
                    var frame = vid.Player?.GetCurrentFrame();
                    if (frame != null)
                    {
                        renderer.DrawGraphic(
                            frame,
                            (int)finalX,
                            (int)finalY,
                            (float)elem.Rotation,
                            (float)elem.ScaleX,
                            (float)elem.ScaleY
                        );
                    }
                }
            }

            // Convert RenderTarget2D → Graphic
            SnapshotGraphic = new Graphic(w, h, target.Bitmap.CopyPixelsToArray());

            SnapshotDirty = false;
        }

        // ---------------------------------------------------------
        // DIPlay calls this to get the final rendered image
        // ---------------------------------------------------------
        public override object? DrawAs()
        {
            return SnapshotGraphic;
        }
    }
}
