using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PaintPower.Editors;
using PaintPower.Editors.Logic;
using PaintPower.Logging;
using Avalonia.Platform;

namespace PaintPower.ProjectSystem;

public class ProjectLoader
{

    public async Task LoadDefaultProject(PaintProject project, ProjectEditorLogic logic)
    {
        // Embedded ZIP inside the application
        var uri = new Uri("avares://PaintPower/src/Assets/Untitled.xPaint");

        // Try to open the embedded ZIP
        if (!AssetLoader.Exists(uri))
        {
            Log.QuickLog("Default Project does not exist!");
            project.Metadata = new ProjectMetadata { name = "Untitled Project" };
            return;
        }

        logic.RefreshUI();

        // Load the project from the embedded ZIP stream
        using var stream = AssetLoader.Open(uri);

        // IMPORTANT: LoadProject expects a file path, but we have a stream.
        // So we temporarily write the ZIP to the workspace.
        string tempZipPath = Path.Combine(project.Workspace.Root, "_default.xPaint");

        Log.QuickLog("Wrote to root.");

        using (var fs = File.Create(tempZipPath))
            stream.CopyTo(fs);

        await logic.LoadProject(tempZipPath, true);

        // IMPORTANT: Reset path so Save asks for a location
        project.ProjectPath = "";
    }

    public void LoadProjectFromSystem() { }
}