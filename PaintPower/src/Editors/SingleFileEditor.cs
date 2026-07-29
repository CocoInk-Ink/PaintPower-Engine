using Avalonia.Controls;

namespace PaintPower.Editors;

public class SingleFileEditor : Editor
{
    public string? FilePath { get; }

    public SingleFileEditor()
    {
        InitializeComponent();
    }

    public SingleFileEditor(string filePath)
    {
        FilePath = filePath;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Content = new TextBlock
        {
            Text = FilePath == null
                ? "New Single File (not implemented)"
                : $"Single File Editor (not implemented)\n{FilePath}",
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
                    new HeaderItem { Label = "Create New File and Open" },
                    new HeaderItem { Label = "Close File and Open" },
                    new HeaderItem { Label = "Close File" },
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
