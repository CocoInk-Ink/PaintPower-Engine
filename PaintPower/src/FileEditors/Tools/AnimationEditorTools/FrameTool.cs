using System;
using Avalonia.Controls;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools;

public class FrameTool
{
    public Action<Canvas> DrawAction { get; set; }

    public FrameTool(Action<Canvas> drawAction)
    {
        DrawAction = drawAction;
    }
}
