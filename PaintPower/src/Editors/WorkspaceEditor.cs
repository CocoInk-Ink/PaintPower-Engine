using System.Linq;
using Avalonia.Controls;

namespace PaintPower.Editors;

public partial class WorkspaceEditor : FolderEditor
{
    public WorkspaceEditor(string? dummy = null) : base(dummy ?? "")
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Content = new TextBlock
        {
            Text = "Workspace Editor (not implemented)",
            Margin = new Avalonia.Thickness(20)
        };
    }

    public override HeaderDefinition GetHeaderDefinition()
    {
        var baseDef = base.GetHeaderDefinition();

        // Insert workspace-specific items at the top of File menu
        var fileMenu = baseDef.Menus["File"];
        baseDef.Menus["File"] = (System.Collections.Generic.List<HeaderItem>)new HeaderItem[]
        {
            new HeaderItem { Label = "Remove Folder from Workspace" },
            new HeaderItem { Label = "Save Workspace (.vsworkspace)" },
            new HeaderItem { Label = "Save Workspace (.pworkspace)" },
            new HeaderItem { IsSeparator = true }
        }.Concat(fileMenu);

        return baseDef;
    }
}
