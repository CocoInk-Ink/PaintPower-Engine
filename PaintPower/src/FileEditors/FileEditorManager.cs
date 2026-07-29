using System;
using System.IO;
using Avalonia.Controls;
using PaintPower.ProjectSystem;

namespace PaintPower.FileEditors;

public class FileEditorManager
{
    private readonly TempWorkspace _workspace;
    private static FileEditor? ActiveEditor = null;

    public FileEditorManager(TempWorkspace workspace)
    {
        _workspace = workspace;
    }

    public FileEditor GetEditorFromFileType(string path)
    {
        // FIX: keep full relative path inside items/
        string relative = Path.GetRelativePath(_workspace.ItemsDir, path);

        var ext = Path.GetExtension(path);
        var type = EditorTypes.FindEditorFromExt(ext.ToLower());

        return ActiveEditor = type switch
        {
            "Paint" => new PaintEditor(relative, _workspace),
            "Script" => new ScriptEditor(relative, _workspace),
            "Animation" => new AnimationEditor(relative, _workspace),
            "Video" => new VideoEditor(relative, _workspace),
            "Sound" => new SoundPlayer(relative, _workspace),
            _ => new FileEditor().addText(new TextBlock { Text = $"Unsupported file: {ext}" })
        };
    }

    // Save items in the editor to the temp directory.
    public static void SaveEditor() {
        ActiveEditor?.Save();
    }

    public void TranslateGUI()
    {
        if (ActiveEditor != null)
            ActiveEditor.TranslateGUI();
    }
}