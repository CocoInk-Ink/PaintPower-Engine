namespace PaintPower.FileExplorer;

public class ExplorerItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }

    public override string ToString()
    {
        return IsDirectory ? $"{Name}/" : Name;
    }
}