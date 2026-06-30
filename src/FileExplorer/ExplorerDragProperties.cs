using Avalonia;

namespace PaintPower.FileExplorer;

public class ExplorerDragProperties
{
    public static readonly AttachedProperty<bool> IsDragHoverProperty =
        AvaloniaProperty.RegisterAttached<ExplorerDragProperties, Avalonia.Controls.Control, bool>("IsDragHover");

    public static void SetIsDragHover(Avalonia.Controls.Control control, bool value) =>
        control.SetValue(IsDragHoverProperty, value);

    public static bool GetIsDragHover(Avalonia.Controls.Control control) =>
        control.GetValue(IsDragHoverProperty);

    public static readonly AttachedProperty<bool> IsDraggingSelfProperty =
        AvaloniaProperty.RegisterAttached<ExplorerDragProperties, Avalonia.Controls.Control, bool>("IsDraggingSelf");

    public static void SetIsDraggingSelf(Avalonia.Controls.Control control, bool value) =>
        control.SetValue(IsDraggingSelfProperty, value);

    public static bool GetIsDraggingSelf(Avalonia.Controls.Control control) =>
        control.GetValue(IsDraggingSelfProperty);
}
