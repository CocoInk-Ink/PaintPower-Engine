using System;

namespace PaintPower.Tools.Dragging;

public interface IDropTarget
{
    void OnDrop(object? payload);
}
