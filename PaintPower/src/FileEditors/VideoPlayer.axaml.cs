using System;
using PaintPower.ProjectSystem;
using Avalonia.Controls;
using Avalonia;

namespace PaintPower.FileEditors;

public partial class VideoPlayer : FileEditor
{
    // Inherits "FullPath" from FileEditor, can also use the path variable passed in the constructor.

    public VideoPlayer(string path, TempWorkspace workspace)
    {
        Content = new TextBlock { Text = $"Video editor placeholder for: {path}" };
    }

    public override void Save() {}
    public override void Open() {}
    public override void Close() {}
    public override void Undo() { }

    public override void Redo() { }

    public override void Copy() { }
    public override void Cut() { }
    public override void Paste() { }

    public override void Activate() { }
}