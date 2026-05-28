using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PaintPower.Logging;

namespace PaintPower.ProjectSystem;

public class ProjectLoader
{
    public void LoadDefaultProject(PaintProject project)
    {
        // Path to your embedded default project
        string defaultZip = "Assets/Untitled.xPaint";

        // Instead of throwing an error, create an empty project if the default zip is missing
        if (!File.Exists(defaultZip))
        {
            Log.QuickLog($"Default project zip not found at {defaultZip}. Creating an empty project.");
            project.Metadata = new ProjectMetadata { name = "Untitled Project" };
            return;
        }

        // Extract into the project's workspace
        ZipFile.ExtractToDirectory(defaultZip, project.Workspace.Root, overwriteFiles: true);

        // Load metadata
        string metaPath = Path.Combine(project.Workspace.Root, "project.json");
        if (File.Exists(metaPath))
        {
            string json = File.ReadAllText(metaPath);
            project.Metadata = JsonSerializer.Deserialize<ProjectMetadata>(json) ?? new ProjectMetadata();
        }

        // Load sprites
        project.LoadSprites();
    }

    public void LoadProjectFromSystem() {}
}