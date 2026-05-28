using PaintPower.Accessibility.Translation;
using PaintPower.Logging;
using System;
using System.IO;
using System.Linq;

namespace PaintPower.ProjectSystem;

public class PaintSprite
{
    public string Name { get; set; } = "";
    public string SpriteFolder { get; set; } = ""; // absolute path in workspace

    public string JsonPath => Path.Combine(SpriteFolder, "Sprite.json");
    public string AnimationPath => Path.Combine(SpriteFolder, "Sprite.wxa");
    public string ThumbnailPath => Path.Combine(SpriteFolder, "Sprite.png");
    public string ScriptPath => Path.Combine(SpriteFolder, "Sprite.pss");
    public string ItemsFolder => Path.Combine(SpriteFolder, "items");

    public override string ToString() => Name;

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