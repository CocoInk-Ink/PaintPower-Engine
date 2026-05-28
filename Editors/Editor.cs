namespace PaintPower.Editors;

using System;
using System.IO;
using Avalonia.Controls;
using PaintPower.ProjectSystem;

public class Editor
{
    private readonly TempWorkspace _workspace;
    private static EditorBase? ActiveEditor = null;

    public Editor(TempWorkspace workspace)
    {
        _workspace = workspace;
    }

    public EditorBase GetEditorFromFileType(string path)
    {
        // FIX: keep full relative path inside items/
        string relative = Path.GetRelativePath(_workspace.ItemsDir, path);

        var ext = Path.GetExtension(path);
        var type = Editors.EditorTypes.FindEditorFromExt(ext.ToLower());

        return ActiveEditor = type switch
        {
            "xPaint" => new EditorPart(),
            "Paint" => new PaintEditor(relative, _workspace),
            "Script" => new ScriptEditor(relative, _workspace),
            "Animation" => new AnimationEditor(relative, _workspace),
            "Video" => new VideoEditor(relative, _workspace),
            "Sound" => new SoundPlayer(relative, _workspace),
            _ => new EditorBase().addText(new TextBlock { Text = $"Unsupported file: {ext}" })
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