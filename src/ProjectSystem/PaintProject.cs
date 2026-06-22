using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PaintPower.Dialogs;
using PaintPower.Editors.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaintPower.ProjectSystem;

/// <summary>
/// Pure project data model.
/// Handles:
///   - Workspace extraction
///   - Metadata load/save
///   - Sprite load/save
///   - ZIP creation
///
/// Does NOT:
///   - Show dialogs
///   - Update UI
///   - Talk to server
///   - Modify window title
///   - Ask user where to save
/// </summary>
public class PaintProject
{
    public string ProjectPath { get; set; } = ""; // Path to .xPaint file
    public TempWorkspace Workspace { get; }
    public ProjectMetadata Metadata { get; set; }

    public List<PaintSprite> Sprites { get; private set; } = new();

    public PaintProject()
    {
        Workspace = new TempWorkspace();
        Metadata = new ProjectMetadata();
    }

    // ------------------------------------------------------------
    // CREATE NEW PROJECT
    // ------------------------------------------------------------
    public void CreateNew(ProjectEditorLogic logic)
    {
        var loader = new ProjectLoader();
        loader.LoadDefaultProject(this, logic);

        ProjectPath = "";
        Metadata = new ProjectMetadata { name = "Untitled", OpenFile = null };

        SaveMetadata();
    }

    // ------------------------------------------------------------
    // LOAD EXISTING PROJECT
    // ------------------------------------------------------------
    public async Task Load(string projectPath, Action<int, int>? onProgress = null)
    {
        ProjectPath = projectPath;

        // Reset workspace
        if (Directory.Exists(Workspace.Root))
            Directory.Delete(Workspace.Root, recursive: true);

        Directory.CreateDirectory(Workspace.Root);
        Directory.CreateDirectory(Workspace.ItemsDir);

        // Extract ZIP
        using (var archive = ZipFile.OpenRead(projectPath))
        {
            int total = archive.Entries.Count;
            int processed = 0;

            foreach (var entry in archive.Entries)
            {
                string destinationPath = Path.Combine(Workspace.Root, entry.FullName);

                if (entry.FullName.EndsWith("/"))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                processed++;
                onProgress?.Invoke(processed, total);
            }
        }

        // Load metadata
        string metaPath = Path.Combine(Workspace.Root, "project.json");
        if (File.Exists(metaPath))
        {
            string json = File.ReadAllText(metaPath);
            Metadata = JsonSerializer.Deserialize<ProjectMetadata>(json) ?? new ProjectMetadata();
        }
        else
        {
            Metadata = new ProjectMetadata();
        }

        // Load sprites
        Sprites.Clear();
        LoadSprites();
    }

    // ------------------------------------------------------------
    // SAVE PROJECT
    // ------------------------------------------------------------
    public async Task SaveToDisk(string? outputPath = null)
    {
        SaveMetadata();

        string target = outputPath ?? ProjectPath;

        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("ProjectPath is empty. UI must provide a save path.");

        await Task.Run(() =>
        {
            if (File.Exists(target))
                File.Delete(target);

            ZipFile.CreateFromDirectory(Workspace.Root, target);
        });

        ProjectPath = target;
    }

    // ------------------------------------------------------------
    // METADATA
    // ------------------------------------------------------------
    public void SaveMetadata()
    {
        string json = JsonSerializer.Serialize(Metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(Workspace.Root, "project.json"), json);
    }

    // ------------------------------------------------------------
    // SPRITES
    // ------------------------------------------------------------
    public void LoadSprites()
    {
        Sprites.Clear();

        string spritesDir = Path.Combine(Workspace.ItemsDir, "sprites");
        if (!Directory.Exists(spritesDir))
            return;

        foreach (var dir in Directory.GetDirectories(spritesDir))
        {
            var sprite = new PaintSprite
            {
                Name = Path.GetFileName(dir),
                SpriteFolder = dir
            };

            sprite.LoadSkins();
            Sprites.Add(sprite);
        }
    }
}

// ------------------------------------------------------------
// PROJECT METADATA
// ------------------------------------------------------------
public class ProjectMetadata
{
    public string name { get; set; } = "Untitled Project";
    public string? OpenFile { get; set; }

    public double? StageWidth { get; set; } = 640;
    public double? StageHeight { get; set; } = 450;

    public string? serverId { get; set; } = null;

    public bool IsLinked()
    {
        if (serverId == "0") return false;
        return !string.IsNullOrEmpty(serverId);
    }
}
