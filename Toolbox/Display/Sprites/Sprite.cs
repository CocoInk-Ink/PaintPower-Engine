// Sprite.cs

using System;
using System.Linq;
using Toolbox.Display.DisplayIntegration;
using Toolbox.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Toolbox.Display.Sprites
{
    public class Sprite : DIItem
    {
        public string Name { get; set; } = "";
        public string InstanceName { get; set; } = "";

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
                this.x = (double)x;
            else
                this.x = Toolbox.Math.Random.calc((StageWidth / 2) * -1, (StageWidth / 2));

            if (y != null)
                this.y = (double)y;
             else
                this.y = Toolbox.Math.Random.calc((StageHeight / 2) * -1, (StageHeight / 2));

            if (skin != null)
                this.CurrentSkinIndex = (int)skin;
            else
                this.CurrentSkinIndex = 0;
        }

        public void SetSkin(int index)
        {
            if (index >= 0 && index < Skins.Count)
            {
                CurrentSkinIndex = index;
                SnapshotDirty = true;
            }
        }

        public static Sprite LoadFromBuildFolder(string spriteFolder, string name, string instanceName)
        {
            var sprite = new Sprite(0, 0)
            {
                Name = name,
                InstanceName = instanceName
            };

            string skinsPath = Path.Combine(spriteFolder, "Skins.xml");
            if (!File.Exists(skinsPath))
                return sprite;

            XDocument document = XDocument.Load(skinsPath);
            XElement? root = document.Root;
            if (root == null)
                throw new InvalidDataException($"Sprite skin file '{skinsPath}' has no root element.");

            foreach (XElement skinNode in root.Elements("Skin"))
            {
                var skin = new RuntimeSkin
                {
                    Name = (string?)skinNode.Attribute("name") ?? "Unnamed",
                    ScriptPath = (string?)skinNode.Attribute("script") ?? ""
                };

                foreach (XElement elementNode in skinNode.Elements())
                {
                    string assetPath = (string?)elementNode.Attribute("asset") ?? "";
                    RuntimeSkinElement? element = null;

                    if (elementNode.Name.LocalName == "Image")
                    {
                        string fullPath = Path.Combine(spriteFolder, assetPath);
                        if (!File.Exists(fullPath))
                            throw new FileNotFoundException($"Sprite image asset '{assetPath}' was not found.", fullPath);

                        object loaded = GraphicLoader.LoadCached(fullPath);
                        if (loaded is not Graphic graphic)
                            throw new InvalidDataException($"Sprite image asset '{fullPath}' did not load as a graphic.");

                        element = new RuntimeImageElement { Graphic = graphic };
                    }
                    else if (elementNode.Name.LocalName == "Video")
                    {
                        string fullPath = Path.Combine(spriteFolder, assetPath);
                        element = new RuntimeVideoElement
                        {
                            Player = VideoPlayer.Load(fullPath),
                            Loop = (bool?)elementNode.Attribute("loop") ?? true,
                            AutoPlay = (bool?)elementNode.Attribute("autoplay") ?? true
                        };
                    }

                    if (element == null)
                        continue;

                    element.InstanceName = (string?)elementNode.Attribute("name") ?? "";
                    element.ZIndex = (int?)elementNode.Attribute("z") ?? 0;
                    element.x = (double?)elementNode.Attribute("x") ?? 0;
                    element.y = (double?)elementNode.Attribute("y") ?? 0;
                    element.Rotation = (double?)elementNode.Attribute("rotation") ?? 90;
                    element.ScaleX = (double?)elementNode.Attribute("scaleX") ?? 1;
                    element.ScaleY = (double?)elementNode.Attribute("scaleY") ?? 1;
                    skin.Elements.Add(element);
                }

                sprite.Skins.Add(skin);
            }

            return sprite;
        }


        // ---------------------------------------------------------
        // Snapshot Rendering
        // ---------------------------------------------------------

        // Drawing should be moved to each element, then the sprite puts it together.
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
                double finalX = elem.x + w / 2;
                double finalY = elem.y + h / 2;

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
