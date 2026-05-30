using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PaintPower.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaintPower.ProjectSystem;

public class PaintProject
{
    public string ProjectPath { get; set; } = ""; // Path to zip file.
    public TempWorkspace Workspace { get; }
    public ProjectMetadata Metadata { get; set; }

    public List<PaintSprite> Sprites { get; private set; } = new(); // Sprite list

    public string ProjectName { get; private set; } = string.Empty;

    public PaintProject()
    {
        Workspace = new TempWorkspace();
        Metadata = new ProjectMetadata();
    }

    // -------------------------
    // CREATE NEW PROJECT
    // -------------------------
    public void CreateNew()
    {
        var loader = new ProjectLoader();
        loader.LoadDefaultProject(this);

        // ProjectPath stays empty → user must Save As
        ProjectPath = "";

        Metadata = new ProjectMetadata { name = "Untitled", OpenFile = null };
        SaveMetadata();
    }

    // -------------------------
    // SAVE NEW PROJECT
    // -------------------------
    public async Task<ProjectLoaderResult> SaveNewProject(Window owner)
    {
        var savePicker = await owner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Create New Project",
                DefaultExtension = "xPaint",
                SuggestedFileName = $"{Metadata.name}.xPaint",
                ShowOverwritePrompt = true
            });

        if (savePicker == null)
        {
            return new ProjectLoaderResult
            {
                Mode = ProjectLoaderMode.New,
                Path = string.Empty
            };
        }

        PaintPower_Engine.window.Title = $"PaintPower - {Metadata.name}";

        return new ProjectLoaderResult
        {
            Mode = ProjectLoaderMode.New,
            Path = savePicker.Path.LocalPath
        };
    }

    // -------------------------
    // LOAD EXISTING PROJECT
    // -------------------------
    public async Task Load(string projectPath)
    {
        ProjectPath = projectPath;

        // Clear any old temp workspace content before extracting a new project.
        if (Directory.Exists(Workspace.Root))
            Directory.Delete(Workspace.Root, recursive: true);

        Directory.CreateDirectory(Workspace.Root);
        Directory.CreateDirectory(Workspace.ItemsDir);

        // Extract ZIP into temp workspace
        ZipFile.ExtractToDirectory(projectPath, Workspace.Root, overwriteFiles: true);

        // Load metadata
        string metaPath = Path.Combine(Workspace.Root, "project.json");
        if (File.Exists(metaPath))
        {
            string json = File.ReadAllText(metaPath);
            try
            {
                Metadata = JsonSerializer.Deserialize<ProjectMetadata>(json) ?? new ProjectMetadata();
            }
            catch
            {
                Metadata = new ProjectMetadata();
            }
        }
        else
        {
            Metadata = new ProjectMetadata();
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {

            if (PaintPower_Engine.App.server.Username != "" && Metadata.IsLinked())
            {
                PaintPower_Engine.App.AskToLinkProject(this);
            }

            // Now that the project is loaded
            Sprites.Clear();
            LoadSprites();
        });
    }

    // -------------------------
    // SAVE PROJECT
    // -------------------------
    public async Task SaveToDisk(string tempPath = "")
    {
        // Always update metadata first
        SaveMetadata();

        // If no path yet -> ask user where to save
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            PaintPower_Engine.App.isNewProject = true; // Keep isNewProject checks

            var result = await SaveNewProject(MainWindow.window);

            if (string.IsNullOrWhiteSpace(result.Path))
                return; // user cancelled

            ProjectPath = result.Path;
            PaintPower_Engine.App.isNewProject = false;
        }

        // Recreate ZIP
        // Run ZIP creation on background thread
        await Task.Run(() =>
        {
            if (tempPath != "")
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                ZipFile.CreateFromDirectory(Workspace.Root, tempPath);
            }
            else
            {
                if (File.Exists(ProjectPath))
                    File.Delete(ProjectPath);

                ZipFile.CreateFromDirectory(Workspace.Root, ProjectPath);
            }
        });
    }

    public void SaveMetadata()
    {
        string json = JsonSerializer.Serialize(Metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(Workspace.Root, "project.json"), json);
    }


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

            // Load sprite skins.
            sprite.LoadSkins();

            Sprites.Add(sprite);
        }
    }
}

// -------------------------
// PROJECT METADATA STRUCT
// -------------------------
public class ProjectMetadata
{
    public string? name { get; set; } = "Untitled Project";
    public string? OpenFile { get; set; }

    public double? StageWidth { get; set; } = 640;
    public double? StageHeight { get; set; } = 450;

    // For online options.
    public string? serverId { get; set; } = null;
    public bool IsLinked()
    {
        if (serverId == "0") return false;
        return !string.IsNullOrEmpty(serverId);
    }
}