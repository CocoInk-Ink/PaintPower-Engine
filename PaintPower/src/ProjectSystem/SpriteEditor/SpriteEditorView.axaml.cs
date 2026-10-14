using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using PaintPower.FileEditors;
using PaintPower.ProjectSystem;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.SoundEffects;

namespace PaintPower.ProjectSystem.SpriteEditor;

public enum SpriteEditorTab
{
    Paint,
    Script,
    Animation,
    Sound,
    VideoPlayer,
    VideoEditor,
    ObjectEditor
}

public partial class SpriteEditorView : SpriteEditor
{
    private readonly PaintSprite _sprite;
    private readonly TempWorkspace _workspace;
    private readonly FileEditorManager _editorManager;
    private FileEditor? Editor;
    private string? path = null;

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

        path = fullPath;

        EditorHost.Content = editor;
    }

    public void CloseEditor()
    {
        EditorHost.Content = null;
        path = null;
    }

    public string MakeSpriteRelative(string fullPath)
    {
        return fullPath.Replace(_sprite.ItemsFolder + Path.DirectorySeparatorChar, "");
    }

    public override void TranslateGUI()
    {
        Explorer.TranslateGUI();
    }

    private void TabStrip_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (path == null)
            return;

        string relative = Path.GetRelativePath(_workspace.ItemsDir, path);

        Log.QuickLog($"Relative Path: {relative}");

        if (sender is TabStrip tabStrip)
        {
            FileEditor? getEditor (int i)
            {
                return i switch
                {
                    (int)SpriteEditorTab.Paint => new PaintEditor(relative, _workspace),
                    (int)SpriteEditorTab.Script => new ScriptEditor(relative, _workspace),
                    (int)SpriteEditorTab.Animation => new AnimationEditor(relative, _workspace),
                    //"Video" => new VideoEditor(relative, _workspace),
                    (int)SpriteEditorTab.Sound => new SoundPlayer(relative, _workspace),
                    _ => null
                };
            };

            var selectedIndex = tabStrip.SelectedIndex;
            var editor = getEditor(selectedIndex);

            if (editor is FileEditor) OpenEditor(editor, path);
        }
    }
}