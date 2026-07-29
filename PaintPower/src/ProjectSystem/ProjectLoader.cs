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
        string defaultZip = "Assets/Untitled.xPaint";

        if (!File.Exists(defaultZip))
        {
            project.Metadata = new ProjectMetadata { name = "Untitled Project" };
            return;
        }

        // Load the default project
        logic.RefreshUI();
        await logic.LoadProject(defaultZip, true);

        // IMPORTANT: Reset path so Save asks for a location
        project.ProjectPath = "";
    }

    public void LoadProjectFromSystem() { }
}