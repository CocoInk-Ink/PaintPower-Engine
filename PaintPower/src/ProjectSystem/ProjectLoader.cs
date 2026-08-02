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
using PaintPower.Plumbing;
using PaintPower.Plumbing.Pipes;

namespace PaintPower.ProjectSystem;

public class ProjectLoader
{

    public async Task LoadDefaultProject(PaintProject project, ProjectEditorLogic logic)
    {
        // Embedded ZIP inside the application
        string filename = "Untitled.xPaint";

        AssetPipe pipe = PaintPower_Engine.App.plumber.GetAssetPipe();

        // Try to open the embedded ZIP
        if (!pipe.AssetExists(filename))
        {
            Log.QuickLog("Default Project does not exist!");
            project.Metadata = new ProjectMetadata { name = "Untitled Project" };
            return;
        }

        logic.RefreshUI();

        await logic.LoadProject(pipe.LoadAsset(filename), true);

        // IMPORTANT: Reset path so Save asks for a location
        project.ProjectPath = "";
    }

    public void LoadProjectFromSystem() { }
}