using System.IO;
using Avalonia.Controls;
using PaintPower.FileEditors;
using PaintPower.ProjectSystem;
using PaintPower.Templates.FileTemplates;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.SoundEffects;

namespace PaintPower.ProjectSystem.SpriteEditor;

public partial class SpriteEditorView : SpriteEditor
{
    private readonly PaintSprite _sprite;
    private readonly TempWorkspace _workspace;
    private readonly FileEditorManager _editorManager;
    private FileEditor? Editor;
    private string? filepath;

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

        filepath = fullPath;
        _currentPage = editor;
    }

    public void CloseEditor()
    {
        filepath = null;
        _currentPage = null;
    }

    public string MakeSpriteRelative(string fullPath)
    {
        return fullPath.Replace(_sprite.ItemsFolder + Path.DirectorySeparatorChar, "");
    }

    public override void TranslateGUI()
    {
        Explorer.TranslateGUI();
    }

    /*---- Editor Tabs ---*/

    private int _selectedIndex;
    private object? _currentPage;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                UpdateCurrentPage();
            }
        }
    }

    public object? CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
        }
    }

    private void UpdateCurrentPage()
    {
        if (filepath == null)
        {
            _currentPage = null;
            return;
        }

        string relative = Path.GetRelativePath(_workspace.ItemsDir, filepath);

        CurrentPage = SelectedIndex switch
        {
            0 => new PaintEditor(relative, _workspace),
            1 => new ScriptEditor(relative, _workspace),
            2 => new AnimationEditor(relative, _workspace),
            _ => null
        };

        if (CurrentPage == null) filepath = null;
    }

    private void TabStrip_SelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateCurrentPage();
}
