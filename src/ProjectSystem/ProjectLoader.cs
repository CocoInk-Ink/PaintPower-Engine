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

namespace PaintPower.ProjectSystem;

public class ProjectLoader
{
    public async Task LoadDefaultProject(PaintProject project, ProjectEditorLogic logic)
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

        // Use normal project loading
        logic.RefreshUI();
        await logic.LoadProject(defaultZip);
    }

    public void LoadProjectFromSystem() {}
}