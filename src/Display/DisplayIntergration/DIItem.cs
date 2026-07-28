using System;
using PaintPower.Editors.Logic;
using PaintPower.ProjectSystem;
using PaintPower.Tools.Graphics;

namespace PaintPower.Display.DisplayIntegration;

public class DIItem
{
    private PaintProject? project => MainWindow.window.mainGui?.projectEditor?.Logic?.Project;

    public double x;
    public double y;

    public double ScaleX = 1f;
    public double ScaleY = 1f;

    public bool IsVisible = true;

    public double Scale
    {
        get => ScaleX;
        set { ScaleX = value; ScaleY = value; }
    }

    public int Z = 0;

    public double Rotation = 90; // 90 is straight (right), 0 (up) 180 (down) -90 (left).
    public double StageWidth => (project == null || project?.Metadata?.StageWidth == null) ? 640 : (double)project.Metadata.StageWidth;
    public double StageHeight => (project == null || project?.Metadata?.StageHeight == null) ? 450 : (double)project.Metadata.StageHeight;

    public DIItem() { }

    // No longer abstract
    public virtual object? DrawAs()
    {
        return null;
    }

}