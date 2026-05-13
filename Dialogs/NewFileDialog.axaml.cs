/*

namespace PaintPower.Templates.FileTemplates;

public class  : FileTemplate
{
    public ()
    {
        filetype = "";
        name = "";
        description = "";
        category = "";
    }
}

*/

using Avalonia.Controls;
using Avalonia.Interactivity;
using PaintPower.Logging;
using PaintPower.Templates;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PaintPower.Dialogs;

public partial class NewFileDialog : Window
{
    public FileList FileTemplates { get; set; } = new FileList();

    public NewFileDialog()
    {
        InitializeComponent();
        FileTemplateList.ItemsSource = FileTemplates.templates;
    }

    public Task<string?> ShowAsync(Window parent)
    {
        // This returns a Task<string?> that completes when Close(result) is called
        return this.ShowDialog<string?>(parent);
    }

    public void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    public async void OnSelectTemplate(object? sender, RoutedEventArgs e)
    {
        FileTemplate? selected = FileTemplateList.SelectedItem as FileTemplate;

        string? originalFilename = FileInputDialog.Text;

        string? ext;
        bool alreadyHasExtension = Path.HasExtension(originalFilename);

        if (alreadyHasExtension)
        {
            ext = Path.GetExtension(originalFilename);
        }
        else if (selected?.isCustom == true)
        {
            var dialog = new InputDialog("Enter custom extension", "Enter a file extension (with dot):");
            ext = await dialog.ShowAsync(this);

            if (string.IsNullOrWhiteSpace(ext))
            {
                // User cancelled or entered an empty extension, default to .txt
                ext = ".txt";
            }

            // Is this an extension with a dot? If not, add it
            if (!ext.StartsWith("."))
            {
                ext = $".{ext}";
            }
        }
        else
        {
            ext = $".{selected?.filetype}";
        }

        string filename = alreadyHasExtension ? $"{Path.GetFileNameWithoutExtension(originalFilename)}{ext}" : $"{originalFilename}{ext}";

        Close(filename);
    }
}