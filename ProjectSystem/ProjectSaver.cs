using Avalonia.Threading;
using PaintPower.Editors;
using PaintPower.Networking;
using Avalonia.Threading;
using System.Threading.Tasks;
using PaintPower.Logging;

// Save to server or on local machine;

namespace PaintPower.ProjectSystem;

class ProjectSaver {
    // Main function that should be called when saving a project.

    public static async Task Save(PaintProject project, EditorBase editor)
    {
        if (editor != null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                editor.Save();
            });
        }

        if (project != null)
            await project.SaveToDisk();
    }

    async public static Task PublishToServer(PaintProject project, EditorBase editor, Server server)
    {
        Log.QuickLog("Preparing to upload to server...");
        await PaintPower_Engine.App.Save();
        if (await server.checkConnection() && project != null)
        {
            Log.QuickLog("Uploading to server...");
            await server.UploadProject(project);
        } else
        {
            Log.QuickLog("Project is null or server is not available! Not uploading!");
        }
    }
}