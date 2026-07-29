using Avalonia.Threading;
using PaintPower.FileEditors;
using System;
using System.Threading.Tasks;

namespace PaintPower.ProjectSystem;

/// <summary>
/// Handles saving the current editor and writing the project to disk.
/// Pure logic — no UI, no engine, no server.
/// </summary>
public static class ProjectSaver
{
    /// <summary>
    /// Saves the active file editor (if any) and writes the project ZIP.
    /// </summary>
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
