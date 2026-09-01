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
using Toolbox.Logging;
using Avalonia.Platform;
using Toolbox.Plumbing;
using Toolbox.Plumbing.Pipes;

namespace PaintPower.ProjectSystem;

public class ProjectLoader
{

    public async Task LoadDefaultProject(PaintProject project, ProjectEditorLogic logic)
    {
        // Embedded ZIP inside the application
        string? path = ResourceKit.Other.Paths.DefaultProject_1;

        // Try to open the embedded ZIP
        if (path == null)
        {
            Log.QuickLog("Default Project does not exist!");
            project.Metadata = new ProjectMetadata { name = "Untitled Project" };
            return;
        }

        logic.RefreshUI();

        if (path != null)
        {
            await logic.LoadProject(path, true);
        }

        // IMPORTANT: Reset path so Save asks for a location
        project.ProjectPath = "";
    }

    public void LoadProjectFromSystem() { }
}