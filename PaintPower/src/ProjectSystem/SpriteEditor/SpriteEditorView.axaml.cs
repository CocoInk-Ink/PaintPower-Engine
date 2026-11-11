using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PaintPower.Dialogs;
using PaintPower.FileEditors;
using PaintPower.ProjectSystem;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;
using Toolbox.SoundEffects;
using System.Linq;
using System.Threading.Tasks;

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
    private int _activeMainTabIndex;
    private int _activeEditorTabIndex = -1;
    private bool _restoringEditorTab;
    private bool _restoringMainTab = true;
    private bool _switchPromptOpen;

    public event Action? SkinChanged;

    public SpriteEditorView(
        PaintSprite sprite,
        TempWorkspace workspace,
        FileEditorManager editorManager,
        double stageWidth = 640,
        double stageHeight = 450)
    {
        InitializeComponent();
        _activeMainTabIndex = MainTabs.SelectedIndex;

        SpriteProperties.LoadSprite(sprite);

        _sprite = sprite;
        _workspace = workspace;
        _editorManager = editorManager;
        _stageWidth = stageWidth;
        _stageHeight = stageHeight;
        SkinList.ItemsSource = _sprite.Skins;

        Explorer.Initialize(_sprite.ItemsFolder, false);

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

    private async void OnFileOpened(string fullPath)
    {
        if (!File.Exists(fullPath))
            return;

        int tabIndex = GetEditorTabIndex(fullPath);
        if (Editor != null)
        {
            if (_switchPromptOpen)
                return;

            if (!await ConfirmEditorSwitchAsync())
                return;
        }

        SetEditorTab(tabIndex);
        var editor = _editorManager.GetEditorFromFileType(fullPath);
        OpenEditor(editor, fullPath);

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

        Editor = editor;
        path = fullPath;

        EditorHost.Content = editor;
    }

    public void CloseEditor()
    {
        EditorHost.Content = null;
        Editor = null;
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

    private async void TabStrip_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_restoringEditorTab || sender is not TabStrip tabStrip)
            return;

        int selectedIndex = tabStrip.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex == _activeEditorTabIndex)
            return;

        if (Editor != null)
        {
            if (_switchPromptOpen || !await ConfirmEditorSwitchAsync())
            {
                RestoreEditorTab();
                return;
            }
        }

        _activeEditorTabIndex = selectedIndex;
        if (path == null)
            return;

        string relative = Path.GetRelativePath(_workspace.ItemsDir, path);
        Log.QuickLog($"Relative Path: {relative}");

        var editor = CreateEditorForTab(selectedIndex, relative);
        if (editor != null)
            OpenEditor(editor, path);
        else
            CloseEditor();
    }

    private async Task<bool> ConfirmEditorSwitchAsync()
    {
        var owner = MainWindow.window;
        if (owner == null)
            return false;

        _switchPromptOpen = true;
        try
        {
            var dialog = new ConfirmEditorSwitchDialog();
            return await dialog.ShowDialog<bool>(owner);
        }
        finally
        {
            _switchPromptOpen = false;
        }
    }

    private void RestoreEditorTab()
    {
        if (_activeEditorTabIndex < 0)
            return;

        _restoringEditorTab = true;
        EditorTabs.SelectedIndex = _activeEditorTabIndex;
        _restoringEditorTab = false;
    }

    private void SetEditorTab(int index)
    {
        _restoringEditorTab = true;
        EditorTabs.SelectedIndex = index;
        _activeEditorTabIndex = index;
        _restoringEditorTab = false;
    }

    private static int GetEditorTabIndex(string fullPath)
    {
        return EditorTypes.FindEditorFromExt(Path.GetExtension(fullPath)) switch
        {
            "Paint" => (int)SpriteEditorTab.Paint,
			"Script" => (int)SpriteEditorTab.Script,
            "Animation" => (int)SpriteEditorTab.Animation,
            "Sound" => (int)SpriteEditorTab.Sound,
            "Video" => (int)SpriteEditorTab.VideoPlayer,
            _ => (int)SpriteEditorTab.Script
        };
    }

    private FileEditor? CreateEditorForTab(int index, string relativePath)
    {
        return index switch
        {
            (int)SpriteEditorTab.Paint => new PaintEditor(relativePath, _workspace),
            (int)SpriteEditorTab.Script => new ScriptEditor(relativePath, _workspace),
            (int)SpriteEditorTab.Animation => new AnimationEditor(relativePath, _workspace),
            (int)SpriteEditorTab.Sound => new SoundPlayer(relativePath, _workspace),
            (int)SpriteEditorTab.VideoPlayer => new VideoEditor(relativePath, _workspace),
            _ => null
        };
    }

    private async void MainTab_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_restoringMainTab)
            return;

        int selectedIndex = MainTabs.SelectedIndex;
        if (selectedIndex == _activeMainTabIndex)
            return;

        int previousIndex = _activeMainTabIndex;
        if (Editor != null)
        {
            if (_switchPromptOpen || !await ConfirmEditorSwitchAsync())
            {
                _restoringMainTab = true;
                MainTabs.SelectedIndex = previousIndex;
                _restoringMainTab = false;
                return;
            }
        }

        _activeMainTabIndex = selectedIndex;
        RefreshMainTab(selectedIndex);
    }

    private void RefreshMainTab(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 0:
                SpriteProperties.LoadSprite(_sprite);
                break;
            case 1:
                Explorer.Initialize(_sprite.ItemsFolder, false);
                if (path != null && File.Exists(path) && _activeEditorTabIndex >= 0)
                {
                    var editor = CreateEditorForTab(
                        _activeEditorTabIndex,
                        Path.GetRelativePath(_workspace.ItemsDir, path));
                    if (editor != null)
                        OpenEditor(editor, path);
                }
                break;
            case 2:
                if (SkinList.SelectedItem is SkinDefinition selectedSkin)
                    LoadSkinEditor(selectedSkin, showThumbnail: false);
                break;
        }
    }

    private void OnSkinSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (SkinList.SelectedItem is not SkinDefinition skin)
        {
            SkinEditorHost.Content = null;
            return;
        }

        LoadSkinEditor(skin, showThumbnail: true);
    }

    private void LoadSkinEditor(SkinDefinition skin, bool showThumbnail)
    {
        if (showThumbnail)
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

        SkinList.SelectedItem = null;
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