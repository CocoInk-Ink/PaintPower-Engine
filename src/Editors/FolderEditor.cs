using Avalonia.Controls;

namespace PaintPower.Editors;

public class FolderEditor : Editor
{
    public string FolderPath { get; }

    public FolderEditor(string folderPath)
    {
        FolderPath = folderPath;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Content = new TextBlock
        {
            Text = $"Folder Editor (not implemented)\n{FolderPath}",
            Margin = new Avalonia.Thickness(20)
        };
    }

    public override HeaderDefinition GetHeaderDefinition()
    {
        return new HeaderDefinition
        {
            Menus = new()
            {
                ["File"] = new()
                {
                    new HeaderItem { Label = "New File" },
                    new HeaderItem { Label = "New Folder" },
                    new HeaderItem { IsSeparator = true },
                    new HeaderItem { Label = "Add Folder to Workspace" },
                    new HeaderItem { IsSeparator = true },
                    new HeaderItem { Label = "Close Editor", Command = () => MainWindow.window.mainGui.CloseEditor() },
                    new HeaderItem { IsSeparator = true },
                    new HeaderItem { Label = "Exit", Command = () => MainWindow.window.Close() }
                },

                ["Edit"] = StandardEditMenu(),
                ["Help"] = StandardHelpMenu(),
                ["Language"] = LanguageMenu()
            }
        };
    }
}
