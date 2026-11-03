using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PaintPower.FileEditors;
using PaintPower.ProjectSystem;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.SoundEffects;
using System.Linq;

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
    private readonly double _stageWidth;
    private readonly double _stageHeight;
    private FileEditor? Editor;
    private string? path = null;

    public event Action? SkinChanged;

    public SpriteEditorView(
        PaintSprite sprite,
        TempWorkspace workspace,
        FileEditorManager editorManager,
        double stageWidth = 640,
        double stageHeight = 450)
    {
        InitializeComponent();

        SpriteProperties.LoadSprite(sprite);

        _sprite = sprite;
        _workspace = workspace;
        _editorManager = editorManager;
        _stageWidth = stageWidth;
        _stageHeight = stageHeight;
        SkinList.ItemsSource = _sprite.Skins;

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

    private void OnSkinSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (SkinList.SelectedItem is not SkinDefinition skin)
        {
            SkinEditorHost.Content = null;
            return;
        }

        SpriteProperties.ShowSkinThumbnail(skin);

        var editor = new SkinEditorView(_sprite, skin, _stageWidth, _stageHeight);
        editor.SkinChanged += () => SkinChanged?.Invoke();
        SkinEditorHost.Content = editor;
    }

    private void OnAddSkin(object? sender, RoutedEventArgs e)
    {
        string baseName = "New Skin";
        string name = baseName;
        int counter = 1;

        while (_sprite.Skins.Any(skin => skin.Name == name))
        {
            name = $"{baseName} {counter}";
            counter++;
        }

        var skin = new SkinDefinition { Name = name };
        _sprite.Skins.Add(skin);
        _sprite.SaveSkins();
        SkinChanged?.Invoke();
        RefreshSkinList(skin);
    }

    private void OnRemoveSkin(object? sender, RoutedEventArgs e)
    {
        if (SkinList.SelectedItem is not SkinDefinition skin)
            return;

        _sprite.Skins.Remove(skin);
        _sprite.SaveSkins();
        SkinChanged?.Invoke();
        RefreshSkinList(_sprite.Skins.FirstOrDefault());
    }

    private void OnRenameSkin(object? sender, RoutedEventArgs e)
    {
        if (SkinList.SelectedItem is not SkinDefinition skin)
            return;

        skin.Name += " Renamed";
        _sprite.SaveSkins();
        SkinChanged?.Invoke();
        RefreshSkinList(skin);
    }

    private void RefreshSkinList(SkinDefinition? selectedSkin)
    {
		if (SkinList == null)
			return;

        SkinList.ItemsSource = null;
        SkinList.ItemsSource = _sprite.Skins;
        SkinList.SelectedItem = selectedSkin;
    }
}