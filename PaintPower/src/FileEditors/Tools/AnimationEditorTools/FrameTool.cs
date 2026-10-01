namespace PaintPower.FileEditors.Tools.AnimationEditorTools;

public class FrameTool
{
    public LayerManagerTool Layers { get; } = new();
    public int Index { get; private set; }

    public void SetFrame(int index)
    {
        Index = index;
    }
}
