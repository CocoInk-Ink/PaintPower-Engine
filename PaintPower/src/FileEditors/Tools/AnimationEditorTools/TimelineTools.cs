using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools;

public class TimelineTool
{
    public int SelectedFrame { get; private set; }
    public int FrameCount { get; private set; }

    public event Action<int>? FrameSelected;

    public void SetFrameCount(int count)
    {
        FrameCount = count;
    }

    public void SelectFrame(int index)
    {
        if (index < 0 || index >= FrameCount)
            return;

        SelectedFrame = index;
        FrameSelected?.Invoke(index);
    }

    public void AddFrame()
    {
        FrameCount++;
    }

    public void RemoveFrame()
    {
        if (FrameCount > 0)
            FrameCount--;
    }
}
