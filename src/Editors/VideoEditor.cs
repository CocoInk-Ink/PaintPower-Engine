namespace PaintPower.Editors;

using PaintPower.ProjectSystem;
using Avalonia.Controls;

public class VideoEditor : EditorBase
{
    public VideoEditor(string path, TempWorkspace workspace)
    {
        Content = new TextBlock { Text = $"Video editor placeholder for: {path}" };
    }
}