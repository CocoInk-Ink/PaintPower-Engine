using Avalonia.Controls;
using PaintPower.Accessibility.Translation;
using PaintPower.Editors;
using PaintPower.Logging;
using PaintPower.ProjectSystem;
using PaintPower.Tools.SoundEffects;
using System.IO;

namespace PaintPower.SpriteEditor;

public partial class SpriteEditorView : UserControl
{
    private readonly PaintSprite _sprite;
    private readonly TempWorkspace _workspace;

    public SpriteEditorView(PaintSprite sprite, TempWorkspace workspace)
    {
        InitializeComponent();

        _sprite = sprite;
        _workspace = workspace;

        Explorer.Initialize(workspace);

        // Set explorer root to sprite items folder
        _workspace.SetActiveRoot(sprite.ItemsFolder);
        Explorer.SetRoot(sprite.ItemsFolder);

        Translator.LanguageChanged += () => TranslateGUI();
    }

    public void OpenEditor(EditorBase editor, string fullPath)
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