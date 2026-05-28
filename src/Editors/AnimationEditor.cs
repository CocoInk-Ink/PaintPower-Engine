namespace PaintPower.Editors;

using PaintPower.ProjectSystem;
using Avalonia.Controls;

public class AnimationEditor : EditorBase
{
    public AnimationEditor(string path, TempWorkspace _workspace)
    {
        Content = new TextBlock { Text = $"Animation editor placeholder for: {path}" };
    }
}