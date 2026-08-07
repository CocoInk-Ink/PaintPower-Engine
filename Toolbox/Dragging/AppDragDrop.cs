using Avalonia.Controls;

namespace Toolbox.Dragging;

public static class AppDragDrop
{
    public static bool IsDragging { get; private set; }
    public static object? Payload { get; private set; }
    public static Control? DragGhost { get; private set; }

    public static void StartDrag(object payload, Control ghost)
    {
        Payload = payload;
        DragGhost = ghost;
        IsDragging = true;
    }

    public static void EndDrag()
    {
        Payload = null;
        DragGhost = null;
        IsDragging = false;
    }
}
