using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PaintPower.Accessibility.Translation;
using PaintPower.Dialogs;
using PaintPower.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PaintPower.FileExplorer;

public partial class ExplorerView : UserControl
{
    public ObservableCollection<ExplorerItem> Items { get; } = new();

    private string _currentDir = "";
    private string? _forcedRoot = null;

    public string ClipboardPath { get; private set; } = "";

    // Events for parent editors
    public event Action<string>? FileOpened;
    public event Action<string>? FolderOpened;
    public event Action? ProjectDirty;

    public ExplorerView()
    {
        InitializeComponent();
        Translator.LanguageChanged += () => Refresh();
    }

    // ------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------
    public void Initialize(string rootFolder)
    {
        if (!Directory.Exists(rootFolder))
            Directory.CreateDirectory(rootFolder);

        _forcedRoot = rootFolder;
        _currentDir = rootFolder;

        FileTree.ItemsSource = Items;
        Refresh();
    }

    public void InitializeMultiple(params string[] roots)
    {
        // WorkspaceEditor: multiple root folders
        _forcedRoot = null; // multi-root mode
        Items.Clear();

        foreach (var root in roots)
        {
            if (Directory.Exists(root))
                Items.Add(BuildTree(root));
        }

        FileTree.ItemsSource = Items;
        BreadcrumbBar.Children.Clear();
    }

    public void SetForcedRoot(string root)
    {
        _forcedRoot = root;
        _currentDir = root;
        Refresh();
    }

    // ------------------------------------------------------------
    // Refresh + Tree Building
    // ------------------------------------------------------------
    private void Refresh()
    {
        TranslateGUI();
        Items.Clear();

        if (_forcedRoot == null)
        {
            // Multi-root workspace mode
            return;
        }

        if (!Directory.Exists(_currentDir))
        {
            return;
        }

        Items.Add(BuildTree(_currentDir));
        UpdateBreadcrumb();

        // Auto-expand root
        FileTree.ApplyTemplate();
        FileTree.UpdateLayout();

        if (FileTree.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem rootItem)
        {
            rootItem.IsExpanded = true;
        }

        ExpandTree();
    }

    private ExplorerItem BuildTree(string path)
    {
        var item = new ExplorerItem
        {
            Name = Path.GetFileName(path),
            FullPath = path,
            IsDirectory = Directory.Exists(path)
        };

        if (item.IsDirectory)
        {
            foreach (var dir in Directory.GetDirectories(path))
                item.Children.Add(BuildTree(dir));

            foreach (var file in Directory.GetFiles(path))
                item.Children.Add(new ExplorerItem
                {
                    Name = Path.GetFileName(file),
                    FullPath = file,
                    IsDirectory = false
                });
        }

        return item;
    }

    private void ExpandAll(ExplorerItem item)
    {
        foreach (var child in item.Children)
            ExpandAll(child);
    }

    private void ExpandTree()
    {
        foreach (var root in Items)
            ExpandAll(root);
    }

    // ------------------------------------------------------------
    // Breadcrumb Bar
    // ------------------------------------------------------------
    private void UpdateBreadcrumb()
    {
        BreadcrumbBar.Children.Clear();

        if (_forcedRoot == null)
            return;

        string root = _forcedRoot;
        string relative = _currentDir.Replace(root, "").TrimStart('\\', '/');

        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);

        string current = root;

        // Root button
        var rootBtn = new Button
        {
            Content = "/",
            Tag = root
        };
        rootBtn.Click += (_, _) =>
        {
            _currentDir = root;
            Refresh();
        };
        BreadcrumbBar.Children.Add(rootBtn);

        foreach (var part in parts)
        {
            current = Path.Combine(current, part);

            var btn = new Button
            {
                Content = part,
                Tag = current
            };

            btn.Click += (_, _) =>
            {
                _currentDir = (string)btn.Tag!;
                Refresh();
            };

            BreadcrumbBar.Children.Add(btn);
        }
    }

    // ------------------------------------------------------------
    // Search
    // ------------------------------------------------------------
    private void OnSearchChanged(object? sender, KeyEventArgs e)
    {
        if (_forcedRoot == null)
            return;

        string query = SearchBox.Text?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            Refresh();
            return;
        }

        Items.Clear();
        Items.Add(SearchTree(_forcedRoot, query));
    }

    private ExplorerItem SearchTree(string path, string query)
    {
        var root = new ExplorerItem
        {
            Name = Path.GetFileName(path),
            FullPath = path,
            IsDirectory = Directory.Exists(path)
        };

        if (root.IsDirectory)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                var child = SearchTree(dir, query);
                if (child.Children.Count > 0 || child.Name.ToLower().Contains(query))
                    root.Children.Add(child);
            }

            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetFileName(file).ToLower().Contains(query))
                {
                    root.Children.Add(new ExplorerItem
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        IsDirectory = false
                    });
                }
            }
        }

        return root;
    }

    // ------------------------------------------------------------
    // Navigation (sandboxed)
    // ------------------------------------------------------------
    private void OnGoRoot(object? sender, RoutedEventArgs e)
    {
        if (_forcedRoot == null)
            return;

        _currentDir = _forcedRoot;
        Refresh();
    }

    private void OnGoUp(object? sender, RoutedEventArgs e)
    {
        if (_forcedRoot == null)
            return;

        if (Path.GetFullPath(_currentDir) == Path.GetFullPath(_forcedRoot))
            return;

        var parent = Directory.GetParent(_currentDir);
        if (parent == null)
            return;

        if (!Path.GetFullPath(parent.FullName).StartsWith(Path.GetFullPath(_forcedRoot)))
            return;

        _currentDir = parent.FullName;
        Refresh();
    }

    // ------------------------------------------------------------
    // File selection + double click
    // ------------------------------------------------------------
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // No action on single click
    }

    private void OnItemDoubleTapped(object? sender, RoutedEventArgs e)
    {
        Log.QuickLog("Item has been clicked.");

        if ((sender as Control)?.DataContext is not ExplorerItem item)
        {
            Log.QuickLog("Sender has no ExplorerItem DataContext.");
            return;
        }

        Log.QuickLog("Still going.");

        if (item.IsDirectory)
        {
            Log.QuickLog("It's a directory.");
            _currentDir = item.FullPath;
            Refresh();
            FolderOpened?.Invoke(item.FullPath);
            return;
        }

        Log.QuickLog("It's a file.");
        FileOpened?.Invoke(item.FullPath);
    }

    // ------------------------------------------------------------
    // File operations (same as before)
    // ------------------------------------------------------------
    private async void OnNewFile(object? sender, RoutedEventArgs e)
    {
        if (_forcedRoot == null)
            return;

        var dialog = new NewFileDialog();
        var window = this.VisualRoot as Window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        string path = Path.Combine(_currentDir, name);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, "");
            ProjectDirty?.Invoke();
        }

        Refresh();
    }

    private async void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        if (_forcedRoot == null)
            return;

        var dialog = new InputDialog("New Folder", "Enter folder name:");
        var window = this.VisualRoot as Window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        string path = Path.Combine(_currentDir, name);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            ProjectDirty?.Invoke();
        }

        Refresh();
    }

    private void OnCopy(object? sender, RoutedEventArgs e)
    {
        if (FileTree.SelectedItem is not ExplorerItem item)
            return;

        ClipboardPath = item.FullPath;
    }

    private void OnCut(object? sender, RoutedEventArgs e)
    {
        if (FileTree.SelectedItem is not ExplorerItem item)
            return;

        ClipboardPath = item.FullPath;
    }

    private async void OnPaste(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ClipboardPath))
            return;

        string filename = Path.GetFileName(ClipboardPath);
        string destPath = Path.Combine(_currentDir, filename);

        try
        {
            if (Directory.Exists(ClipboardPath))
                CopyDirectory(ClipboardPath, destPath);
            else if (File.Exists(ClipboardPath))
                File.Copy(ClipboardPath, destPath);

            ProjectDirty?.Invoke();
            Refresh();
        }
        catch
        {
            var window = this.VisualRoot as Window;
            await new PopupWindowDialog("Error", "Could not paste item", "").ShowAsync(window);
        }
    }

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (FileTree.SelectedItem is not ExplorerItem item)
            return;

        var dialog = new DeletionConfirmationDialog();
        var window = this.VisualRoot as Window;
        var doDelete = await dialog.ShowAsync(window);

        if (doDelete == "delete")
        {
            if (item.IsDirectory)
                Directory.Delete(item.FullPath, true);
            else
                File.Delete(item.FullPath);

            ProjectDirty?.Invoke();
            Refresh();
        }
    }

    private async void OnRename(object? sender, RoutedEventArgs e)
    {
        if (FileTree.SelectedItem is not ExplorerItem item)
            return;

        var dialog = new InputDialog("Rename", $"Enter new name for \"{item.Name}\":");
        var window = this.VisualRoot as Window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        string destPath = Path.Combine(Path.GetDirectoryName(item.FullPath)!, name);

        if (!File.Exists(destPath) && !Directory.Exists(destPath))
        {
            if (item.IsDirectory)
                Directory.Move(item.FullPath, destPath);
            else
                File.Move(item.FullPath, destPath);

            ProjectDirty?.Invoke();
        }

        Refresh();
    }

    private async void OnDuplicate(object? sender, RoutedEventArgs e)
    {
        if (FileTree.SelectedItem is not ExplorerItem item)
            return;

        string nameWithoutExt = Path.GetFileNameWithoutExtension(item.Name);
        string ext = Path.GetExtension(item.Name);
        string newName = $"{nameWithoutExt}_copy{ext}";
        string destPath = Path.Combine(_currentDir, newName);

        int i = 1;
        while (File.Exists(destPath) || Directory.Exists(destPath))
        {
            newName = $"{nameWithoutExt}_copy{i}{ext}";
            destPath = Path.Combine(_currentDir, newName);
            i++;
        }

        if (item.IsDirectory)
            CopyDirectory(item.FullPath, destPath);
        else
            File.Copy(item.FullPath, destPath);

        ProjectDirty?.Invoke();
        Refresh();
    }

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { AllowMultiple = true };
        var window = this.VisualRoot as Window;
        var result = await dialog.ShowAsync(window);

        if (result != null)
        {
            foreach (var file in result)
            {
                string destPath = Path.Combine(_currentDir, Path.GetFileName(file));
                if (!File.Exists(destPath))
                {
                    File.Copy(file, destPath);
                    ProjectDirty?.Invoke();
                }
            }
        }

        Refresh();
    }

    private async void OnExport(object? sender, RoutedEventArgs e)
    {
        if (FileTree.SelectedItem is not ExplorerItem item)
            return;

        var dialog = new SaveFileDialog { InitialFileName = item.Name };
        var window = this.VisualRoot as Window;
        var result = await dialog.ShowAsync(window);

        if (!string.IsNullOrEmpty(result))
        {
            if (item.IsDirectory)
                CopyDirectory(item.FullPath, result);
            else
                File.Copy(item.FullPath, result);
        }
    }

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------
    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));

        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }

    public void TranslateGUI()
    {
        NewFileButton.Header = Translator.Map("New");
        NewFolderButton.Header = Translator.Map("New Folder");
        GoRootButton.Header = Translator.Map("Go to Root");
        GoUpButton.Header = Translator.Map("Go Up");

        CopyButton.Header = Translator.Map("Copy");
        CutButton.Header = Translator.Map("Cut");
        PasteButton.Header = Translator.Map("Paste");
        DeleteButton.Header = Translator.Map("Delete");
        RenameButton.Header = Translator.Map("Rename");
        DuplicateButton.Header = Translator.Map("Duplicate");
        ImportButton.Header = Translator.Map("Import");
        ExportButton.Header = Translator.Map("Export");
    }
}
