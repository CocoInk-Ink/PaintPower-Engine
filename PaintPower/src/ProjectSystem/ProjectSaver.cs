using Avalonia.Threading;
using PaintPower.FileEditors;
using System;
using System.Threading.Tasks;

namespace PaintPower.ProjectSystem;

public static class ProjectSaver
{
    public static async Task Save(PaintProject project, FileEditor? editor, Action<int, int>? onProgress)
    {
        // Save the active editor (UI thread)
        if (editor != null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                editor.Save();
            });
        }

        // Save project to disk
        if (project != null)
            await project.SaveToDisk(null, onProgress);
    }
}
