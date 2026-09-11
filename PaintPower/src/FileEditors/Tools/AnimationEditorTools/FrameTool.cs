using System;
using Avalonia.Controls;

public class FrameTool
{
    public Action<Canvas> DrawAction { get; set; }

    public FrameTool(Action<Canvas> drawAction)
    {
        DrawAction = drawAction;
    }
}
