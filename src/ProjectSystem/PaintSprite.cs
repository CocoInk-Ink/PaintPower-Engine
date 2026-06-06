using PaintPower.Accessibility.Translation;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Logging;
using PaintPower.Sprites;
using PaintPower.Tools.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PaintPower.ProjectSystem;

public class PaintSprite
{
    public string Name { get; set; } = "";
    public string SpriteFolder { get; set; } = ""; // absolute path in workspace

    public string JsonPath => Path.Combine(SpriteFolder, "Sprite.json");
    public string AnimationPath => Path.Combine(SpriteFolder, "Sprite.wxa");

    // Thumbnail.
    public string ThumbnailPath => Path.Combine(SpriteFolder, "Sprite.png");
    public string ScriptPath => Path.Combine(SpriteFolder, "Sprite.pss");

    // NEW: Skins.xml, I guess we will link skins to files in the items folder or it's subdirectories.
    // Please not JSON, i've been waiting for an excuse to use xml!
    public string SkinsPath => Path.Combine(SpriteFolder, "Skins.xml");

    // Files.
    public string ItemsFolder => Path.Combine(SpriteFolder, "items");

    public override string ToString() => Name;

    // Skins //
    public List<SkinDefinition> Skins { get; private set; } = new();

    public void LoadSkins()
    {
        Skins.Clear();

        if (!File.Exists(SkinsPath))
            return;

        var doc = XDocument.Load(SkinsPath);

        foreach (var skinNode in doc.Root.Elements("Skin"))
        {
            var skin = new SkinDefinition
            {
                Name = (string)skinNode.Attribute("name") ?? "Unnamed",
                ScriptPath = (string)skinNode.Attribute("script") ?? ""
            };

            foreach (var elemNode in skinNode.Elements())
            {
                SkinElement elem = null;

                switch (elemNode.Name.LocalName)
                {
                    case "Image":
                        elem = new SkinImageElement
                        {
                            AssetPath = (string)elemNode.Attribute("asset") ?? ""
                        };
                        break;

                    case "Video":
                        elem = new SkinVideoElement
                        {
                            AssetPath = (string)elemNode.Attribute("asset") ?? "",
                            Loop = (bool?)elemNode.Attribute("loop") ?? true,
                            AutoPlay = (bool?)elemNode.Attribute("autoplay") ?? true
                        };
                        break;

                    default:
                        continue; // unknown element type
                }

                // Shared properties
                elem.Id = (string)elemNode.Attribute("id") ?? Guid.NewGuid().ToString();
                elem.InstanceName = (string)elemNode.Attribute("name") ?? "";
                elem.ZIndex = (int?)elemNode.Attribute("z") ?? 0;

                elem.Transform.X = (double?)elemNode.Attribute("x") ?? 0;
                elem.Transform.Y = (double?)elemNode.Attribute("y") ?? 0;
                elem.Transform.Rotation = (double?)elemNode.Attribute("rotation") ?? 0;
                elem.Transform.ScaleX = (double?)elemNode.Attribute("scaleX") ?? 1;
                elem.Transform.ScaleY = (double?)elemNode.Attribute("scaleY") ?? 1;

                skin.Elements.Add(elem);
            }

            Skins.Add(skin);
        }
    }

    public void SaveSkins()
    {
        var doc = new XDocument(
            new XElement("Skins",
                Skins.Select(skin =>
                    new XElement("Skin",
                        new XAttribute("name", skin.Name),
                        new XAttribute("script", skin.ScriptPath ?? ""),

                        skin.Elements.Select(elem =>
                        {
                            XElement node;

                            if (elem is SkinImageElement img)
                            {
                                node = new XElement("Image",
                                    new XAttribute("asset", img.AssetPath ?? "")
                                );
                            }
                            else if (elem is SkinVideoElement vid)
                            {
                                node = new XElement("Video",
                                    new XAttribute("asset", vid.AssetPath ?? ""),
                                    new XAttribute("loop", vid.Loop),
                                    new XAttribute("autoplay", vid.AutoPlay)
                                );
                            }
                            else
                            {
                                return null;
                            }

                            // Shared attributes
                            node.Add(
                                new XAttribute("id", elem.Id),
                                new XAttribute("name", elem.InstanceName ?? ""),
                                new XAttribute("z", elem.ZIndex),
                                new XAttribute("x", elem.Transform.X),
                                new XAttribute("y", elem.Transform.Y),
                                new XAttribute("rotation", elem.Transform.Rotation),
                                new XAttribute("scaleX", elem.Transform.ScaleX),
                                new XAttribute("scaleY", elem.Transform.ScaleY)
                            );

                            return node;
                        })
                    )
                )
            )
        );

        doc.Save(SkinsPath);
    }

    // -----------
    // The Bridge
    // (Oh yeah!)
    // -----------
    public Sprite ToRuntimeSprite()
    {
        var runtime = new Sprite();

        foreach (var skinDef in Skins)
        {
            var rSkin = new RuntimeSkin
            {
                Name = skinDef.Name,
                ScriptPath = skinDef.ScriptPath
            };

            foreach (var elem in skinDef.Elements)
            {
                if (elem is SkinImageElement img)
                {
                    string full = Path.Combine(SpriteFolder, img.AssetPath);
                    var graphic = GraphicLoader.LoadCached(full);

                    rSkin.Elements.Add(new RuntimeImageElement
                    {
                        InstanceName = elem.InstanceName,
                        X = elem.Transform.X,
                        Y = elem.Transform.Y,
                        Rotation = elem.Transform.Rotation,
                        ScaleX = elem.Transform.ScaleX,
                        ScaleY = elem.Transform.ScaleY,
                        ZIndex = elem.ZIndex,
                        Graphic = (Graphic)graphic
                    });
                }
                else if (elem is SkinVideoElement vid)
                {
                    string full = Path.Combine(SpriteFolder, vid.AssetPath);
                    var player = VideoPlayer.Load(full);

                    rSkin.Elements.Add(new RuntimeVideoElement
                    {
                        InstanceName = elem.InstanceName,
                        X = elem.Transform.X,
                        Y = elem.Transform.Y,
                        Rotation = elem.Transform.Rotation,
                        ScaleX = elem.Transform.ScaleX,
                        ScaleY = elem.Transform.ScaleY,
                        ZIndex = elem.ZIndex,
                        Player = player,
                        Loop = vid.Loop,
                        AutoPlay = vid.AutoPlay
                    });
                }
            }

            runtime.Skins.Add(rSkin);
        }

        return runtime;
    }

    // ---------------------------------------------------------
    // STATIC OPERATIONS (SpriteManagerView passes the sprite)
    // ---------------------------------------------------------

    public static void Delete(PaintSprite sprite)
    {
        if (sprite == null)
            return;

        if (Directory.Exists(sprite.SpriteFolder))
            Directory.Delete(sprite.SpriteFolder, recursive: true);
    }

    public static void Rename(PaintSprite sprite, string newName)
    {
        if (sprite == null)
            return;

        string parent = Directory.GetParent(sprite.SpriteFolder)!.FullName;
        string newFolder = Path.Combine(parent, newName);

        Directory.Move(sprite.SpriteFolder, newFolder);

        sprite.SpriteFolder = newFolder;
        sprite.Name = newName;
    }

    public static string? SafeRename(string baseName, string parentFolder)
    {
        // Clean filename to prevent errors, instead of replacing with underscores, we can just remove invalid chars
        var invalidChars = Path.GetInvalidFileNameChars();
        baseName = string.Concat(baseName.Where(c => !invalidChars.Contains(c)));

        if (string.IsNullOrWhiteSpace(baseName))
            return null;

        // If no conflict, return as-is
        string target = Path.Combine(parentFolder, baseName);
        if (!Directory.Exists(target))
            return baseName;

        // Extract trailing number (if any)
        int number = 1;
        string nameWithoutNumber = baseName;

        int i = baseName.Length - 1;
        while (i >= 0 && char.IsDigit(baseName[i]))
            i--;

        if (i < baseName.Length - 1)
        {
            string digits = baseName[(i + 1)..];
            nameWithoutNumber = baseName[..(i + 1)];

            if (int.TryParse(digits, out int parsed))
                number = parsed + 1;
        }

        // Try incrementing until we find a free name
        string newName;
        do
        {
            newName = $"{nameWithoutNumber}{number}";
            number++;
        }
        while (Directory.Exists(Path.Combine(parentFolder, newName)));

        return newName;
    }

    public static PaintSprite Duplicate(PaintSprite sprite)
    {
        if (sprite == null)
            throw new ArgumentNullException(nameof(sprite));

        string parent = Directory.GetParent(sprite.SpriteFolder)!.FullName;

        // Generate a safe name like "Sprite2", "Sprite3", etc.
        string newName = SafeRename(sprite.Name, parent);

        string newFolder = Path.Combine(parent, newName);

        // Copy entire folder recursively
        CopyDirectory(sprite.SpriteFolder, newFolder);

        // Create new sprite instance
        var newSprite = new PaintSprite
        {
            Name = newName,
            SpriteFolder = newFolder
        };

        return newSprite;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string dest = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }

        // Copy subdirectories
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string dest = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }
}