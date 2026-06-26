// ExplorerRow.cs

using System;

namespace PaintPower.FileExplorer;

public class ExplorerRow
{
    public ExplorerItem Item { get; set; }
    public int Depth { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsRenaming { get; set; } = false;
}