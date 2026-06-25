// ExplorerItem.cs

using System.Collections.ObjectModel;

namespace PaintPower.FileExplorer;

public class ExplorerItem
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public bool IsExpanded { get; set; }

    // Children for TreeView
    public ObservableCollection<ExplorerItem> Children { get; set; } = new();

    private readonly ObservableCollection<ExplorerRow> _rows = new();
    public ObservableCollection<ExplorerRow> Rows => _rows;

    // Nullable icon path (ExplorerView template can ignore it)
    public string? Icon { get; set; } = null;

    public override string ToString()
    {
        return IsDirectory ? $"{Name}/" : Name;
    }
}
