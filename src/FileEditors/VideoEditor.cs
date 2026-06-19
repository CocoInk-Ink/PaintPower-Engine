using PaintPower.ProjectSystem;
using Avalonia.Controls;

namespace PaintPower.FileEditors;

public class VideoEditor : FileEditor
{
    public VideoEditor(string path, TempWorkspace workspace)
    {
        Content = new TextBlock { Text = $"Video editor placeholder for: {path}" };
    }
}