using PaintPower.ProjectSystem;
using Avalonia.Controls;

namespace PaintPower.FileEditors;

public class AnimationEditor : FileEditor
{
    public AnimationEditor(string path, TempWorkspace _workspace)
    {
        Content = new TextBlock { Text = $"Animation editor placeholder for: {path}" };
    }
}