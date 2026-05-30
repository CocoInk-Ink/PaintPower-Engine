using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaintPower.SpriteEditor;

public partial class ChooseSpriteFileDialog : Window
{
    private readonly string _root;

    public string? SelectedFile { get; private set; }

    public ChooseSpriteFileDialog(string itemsFolder)
    {
        InitializeComponent();
        _root = itemsFolder;

        BuildFolderTree();
        CancelButton.Click += (_, __) => Close(null);
        ChooseButton.Click += (_, __) =>
        {
            if (FileList.SelectedItem is SpriteFileItem item)
                Close(item.RelativePath);
            else
                Close(null);
        };
    }

    private void BuildFolderTree()
    {
        var rootNode = BuildNode(_root);
        FolderTree.ItemsSource = new[] { rootNode };
        FolderTree.SelectionChanged += (_, __) =>
        {
            if (FolderTree.SelectedItem is TreeViewItem node)
                LoadFiles(node.Tag!.ToString()!);
        };
    }

    private TreeViewItem BuildNode(string path)
    {
        var node = new TreeViewItem
        {
            Header = Path.GetFileName(path),
            Tag = path
        };

        foreach (var dir in Directory.GetDirectories(path))
            node.Items.Add(BuildNode(dir));

        return node;
    }

    private void LoadFiles(string folder)
    {
        var files = Directory.GetFiles(folder)
            .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg") || f.EndsWith(".gif"))
            .Select(f => new SpriteFileItem
            {
                Name = Path.GetFileName(f),
                RelativePath = f.Substring(_root.Length + 1).Replace("\\", "/"),
                Thumbnail = LoadThumbnail(f)
            })
            .ToList();

        FileList.ItemsSource = files;
    }

    private Bitmap? LoadThumbnail(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private void OnFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (FileList.SelectedItem is SpriteFileItem item)
        {
            string fullPath = Path.Combine(_root, item.RelativePath);
            try
            {
                PreviewImage.Source = new Bitmap(fullPath);
            }
            catch
            {
                PreviewImage.Source = null;
            }
        }
        else
        {
            PreviewImage.Source = null;
        }
    }

    public async Task<string?> ShowDialogAsync(Window owner)
    {
        return await this.ShowDialog<string?>(owner);
    }
}


public class SpriteFileItem
{
    public string Name { get; set; }
    public string RelativePath { get; set; }
    public Bitmap? Thumbnail { get; set; }
}
