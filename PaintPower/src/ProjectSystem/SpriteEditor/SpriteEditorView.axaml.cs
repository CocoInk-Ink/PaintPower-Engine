using Avalonia.Controls;
using Toolbox.Accessibility.Translation;
using PaintPower.FileEditors;
using Toolbox.Logging;
using PaintPower.ProjectSystem;
using Toolbox.SoundEffects;
using System.IO;

namespace PaintPower.ProjectSystem.SpriteEditor;

public partial class SpriteEditorView : SpriteEditor
{
    private readonly PaintSprite _sprite;
    private readonly TempWorkspace _workspace;
    private readonly FileEditorManager _editorManager;
    private FileEditor? Editor;

    public SpriteEditorView(PaintSprite sprite, TempWorkspace workspace, FileEditorManager editorManager)
    {
        InitializeComponent();

        _sprite = sprite;
        _workspace = workspace;
        _editorManager = editorManager;

        // NEW: Initialize Explorer directly with the sprite folder
        Explorer.Initialize(_sprite.ItemsFolder, false);

        // Sandbox the explorer to this folder
        Explorer.SetForcedRoot(_sprite.ItemsFolder);

        Explorer.FileOpened += OnFileOpened;

        Explorer.FileRemoved += path =>
{
    if (Editor != null && Editor.FullPath == path)
    {
        Editor = null;
        CloseEditor();
    }
};

        Explorer.FileMoved += (oldPath, newPath) =>
{
    if (Editor != null && Editor.FullPath == oldPath)
    {
        Editor.SetFullPath(newPath);
        Editor.SetRelativePath(MakeSpriteRelative(newPath));
    }
};


        Translator.LanguageChanged += () => TranslateGUI();
    }

    private void OnFileOpened(string fullPath)
    {
        if (!File.Exists(fullPath))
            return;

        Editor = _editorManager.GetEditorFromFileType(fullPath);

        if (Editor != null)
            OpenEditor(Editor, fullPath);

        else CloseEditor();
    }

    public void OpenEditor(FileEditor editor, string fullPath)
    {
        SoundEffects.Click.Play();
        TranslateGUI();

        var relative = MakeSpriteRelative(fullPath);
        editor.SetRelativePath(relative);
        editor.SetFullPath(fullPath);

        Log.QuickLog(fullPath);
        Log.QuickLog(relative);

        editor.Activate();

        EditorHost.Content = editor;
    }

    public void CloseEditor()
    {
        EditorHost.Content = null;
    }

    public string MakeSpriteRelative(string fullPath)
    {
        return fullPath.Replace(_sprite.ItemsFolder + Path.DirectorySeparatorChar, "");
    }

    public void TranslateGUI()
    {
        Explorer.TranslateGUI();
    }
}
