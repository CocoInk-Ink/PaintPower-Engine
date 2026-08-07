using System;

namespace Toolbox.Dragging;

public interface IDropTarget
{
    void OnDrop(object? payload);
}
