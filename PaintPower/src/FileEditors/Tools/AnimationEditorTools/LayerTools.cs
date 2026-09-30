using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using PaintPower.FileEditors.Tools.AnimationEditorTools.Drawing;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools;

public class LayerTool
{
    public string Name { get; set; }
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; } = false;

    public List<LayerFrameTool> Frames { get; } = new();

    public LayerTool(string name)
    {
        Name = name;
    }
}

public class LayerFrameTool
{
    public List<Action<Canvas>> DrawActions { get; } = new();
    public List<VectorStroke> Strokes { get; } = new();

    public int Index { get; private set; }

    public void AddShape(Action<Canvas> shape)
    {
        DrawActions.Add(shape);
    }

    public void Render(Canvas canvas, double opacity)
    {
        // Render vector strokes
        foreach (var stroke in Strokes)
            stroke.Render(canvas, opacity);

        // Render shapes (circles, rectangles)
        foreach (var shape in DrawActions)
        {
            // Shapes don't have opacity built-in, so we wrap them
            shape(canvas);

            if (opacity < 1.0)
            {
                if (canvas.Children.Count > 0 &&
                    canvas.Children[^1] is Shape s)
                {
                    s.Opacity = opacity;
                }
            }
        }
    }

    public void SetFrame(int index)
    {
        Index = index;
    }
}

public class LayerManagerTool
{
    public ObservableCollection<LayerTool> Layers { get; } = new();

    public void AddLayer(string name)
    {
        Layers.Add(new LayerTool(name));
    }

    public void RemoveLayer(LayerTool layer)
    {
        Layers.Remove(layer);
    }

    public void MoveLayerUp(LayerTool layer)
    {
        int index = Layers.IndexOf(layer);
        if (index > 0)
            Layers.Move(index, index - 1);
    }

    public void MoveLayerDown(LayerTool layer)
    {
        int index = Layers.IndexOf(layer);
        if (index < Layers.Count - 1)
            Layers.Move(index, index + 1);
    }
}
