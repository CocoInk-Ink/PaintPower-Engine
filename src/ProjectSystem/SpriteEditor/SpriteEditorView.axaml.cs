using Avalonia.Controls;
using PaintPower.Accessibility.Translation;
using PaintPower.FileEditors;
using PaintPower.Logging;
using PaintPower.ProjectSystem;
using PaintPower.Tools.SoundEffects;
using System.IO;

namespace PaintPower.ProjectSystem.SpriteEditor;

public partial class SpriteEditorView : SpriteEditor
{
    private readonly PaintSprite _sprite;
    private readonly TempWorkspace _workspace;

    private readonly FileEditorManager _editorManager;

    public SpriteEditorView(PaintSprite sprite, TempWorkspace workspace, FileEditorManager editorManager)
    {
        InitializeComponent();

        _sprite = sprite;
        _workspace = workspace;
        _editorManager = editorManager;

        Explorer.Initialize(workspace);
        Explorer.SetForcedRoot(sprite.ItemsFolder);

        Explorer.FileOpened += OnFileOpened;

        Translator.LanguageChanged += () => TranslateGUI();
    }

    private void OnFileOpened(string fullPath)
    {
        if (!File.Exists(fullPath))
            return;

        // Ask FileEditorManager for the correct editor
        var editor = _editorManager.GetEditorFromFileType(fullPath);

        if (editor != null)
            OpenEditor(editor, fullPath);
    }


    public void OpenEditor(FileEditor editor, string fullPath)
    {
        SoundEffects.Click.Play();
        TranslateGUI();
        var relative = MakeSpriteRelative(fullPath);
        editor.SetRelativePath(relative);
        Log.QuickLog(fullPath);
        Log.QuickLog(relative);
        EditorHost.Content = editor;
    }

    public string MakeSpriteRelative(string fullPath)
    {
        // fullPath = items/sprites/<Sprite>/items/<whatever>
        return fullPath.Replace(_sprite.ItemsFolder + Path.DirectorySeparatorChar, "");
    }

    public void TranslateGUI()
    {
        Explorer.TranslateGUI();
    }
}