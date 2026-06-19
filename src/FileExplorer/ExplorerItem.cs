using System.Collections.ObjectModel;

namespace PaintPower.FileExplorer;

public class ExplorerItem
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; }

    // Children for TreeView
    public ObservableCollection<ExplorerItem> Children { get; set; } = new();

    // Nullable icon path (ExplorerView template can ignore it)
    public string? Icon { get; set; } = null;

    public override string ToString()
    {
        return IsDirectory ? $"{Name}/" : Name;
    }
}
