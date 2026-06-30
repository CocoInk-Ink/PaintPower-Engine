using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PaintPower.Accessibility.Translation;
using PaintPower.Dialogs;
using PaintPower.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PaintPower.FileExplorer;

#pragma warning disable IDE0047
#pragma warning disable IDE0048

public partial class ExplorerView : UserControl
{
    public bool isReadOnly = false;

    public ObservableCollection<ExplorerRow> Rows { get; } = new();
    public ObservableCollection<ExplorerItem> Items { get; } = new();

    private string _currentDir = "";
    private string? _forcedRoot = null;

    public string ClipboardPath { get; private set; } = "";
    private bool _clipboardIsCut = false;

    public event Action<string>? FileOpened;
    public event Action<string>? FolderOpened;
    public event Action? ProjectDirty;
    public event Action<string>? FileRemoved;
    public event Action<string, string>? FileMoved;

    // custom drag state (local to explorer)
    private bool _isDragging;
    private ExplorerRow? _dragRow;

    public ExplorerView()
    {
        InitializeComponent();
        Translator.LanguageChanged += () => Refresh();
        DataContext = this;
    }

    // ------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------
    public void Initialize(string rootFolder, bool isReadOnly)
    {
        this.isReadOnly = isReadOnly;

        if (!Directory.Exists(rootFolder))
            Directory.CreateDirectory(rootFolder);

        _forcedRoot = rootFolder;
        _currentDir = rootFolder;

        Refresh();
    }

    public void InitializeMultiple(params string[] roots)
    {
        _forcedRoot = null;
        Items.Clear();

        foreach (var root in roots)
        {
            if (Directory.Exists(root))
                Items.Add(BuildTree(root));
        }

        RefreshRows();
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
    private HashSet<string> GetExpandedPaths()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Walk(ExplorerItem item)
        {
            if (item.IsDirectory && item.IsExpanded)
                set.Add(item.FullPath);

            foreach (var child in item.Children)
                Walk(child);
        }

        foreach (var root in Items)
            Walk(root);

        return set;
    }

    private void Refresh()
    {
        TranslateGUI();

        Menus.IsVisible = !isReadOnly;

        // capture current expansion state
        var expanded = GetExpandedPaths();

        Items.Clear();

        if (_forcedRoot == null)
        {
            RefreshRows();
            return;
        }

        if (!Directory.Exists(_currentDir))
            return;

        Items.Add(BuildTree(_currentDir, expanded));
        RefreshRows();
    }

    private ExplorerItem? FindItemByPath(string path)
    {
        ExplorerItem? result = null;

        void Walk(ExplorerItem item)
        {
            if (item.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                result = item;
                return;
            }

            foreach (var child in item.Children)
                Walk(child);
        }

        foreach (var root in Items)
            Walk(root);

        return result;
    }

    private ExplorerItem BuildTree(string path, HashSet<string>? expanded = null)
    {
        var item = new ExplorerItem
        {
            Name = Path.GetFileName(path),
            FullPath = path,
            IsDirectory = Directory.Exists(path),
            IsExpanded = expanded != null && expanded.Contains(path)
        };

        if (item.IsDirectory)
        {
            foreach (var dir in Directory.GetDirectories(path))
                item.Children.Add(BuildTree(dir, expanded));

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

    private void RefreshRows()
    {
        Rows.Clear();
        foreach (var item in Items)
            AddItemRecursive(item, 0);
    }

    private void AddItemRecursive(ExplorerItem item, int depth)
    {
        Rows.Add(new ExplorerRow
        {
            Item = item,
            Depth = depth,
            IsExpanded = item.IsDirectory && item.IsExpanded
        });

        if (item.IsDirectory && item.IsExpanded)
        {
            foreach (var child in item.Children)
                AddItemRecursive(child, depth + 1);
        }
    }

    private void SelectPath(string fullPath)
    {
        var row = Rows.FirstOrDefault(r =>
            r.Item.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));

        if (row != null)
        {
            FileList.SelectedItem = row;
            FileList.ScrollIntoView(row);
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
        RefreshRows();
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
    // Expand / Collapse
    // ------------------------------------------------------------
    private void OnArrowClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control c || c.DataContext is not ExplorerRow row)
            return;

        if (!row.Item.IsDirectory)
            return;

        row.Item.IsExpanded = !row.Item.IsExpanded;
        RefreshRows();
    }

    // ------------------------------------------------------------
    // Custom drag (inside explorer only)
    // ------------------------------------------------------------
    [Obsolete]
    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control c || c.DataContext is not ExplorerRow row)
            return;

        if (!e.GetCurrentPoint(c).Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        _dragRow = row;

        row.IsDraggingSelf = true;

        var container = FileList.ItemContainerGenerator.ContainerFromIndex(Rows.IndexOf(row)) as ListBoxItem;
        if (container != null)
            container.Classes.Add("dragging-self");

        DragGhost.Text = row.Item.Name;
        DragGhost.IsVisible = true;

        var pos = e.GetPosition(FileList);

        Canvas.SetLeft(DragGhost, pos.X + 12);
        Canvas.SetTop(DragGhost, pos.Y + 12);
    }

    private void OnRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _dragRow == null)
            return;

        var pos = e.GetPosition(FileList);
        var hit = FileList.InputHitTest(pos) as Control;

        Canvas.SetLeft(DragGhost, pos.X + 12);
        Canvas.SetTop(DragGhost, pos.Y + 12);

        // Clear previous hover states
        foreach (var r in Rows)
        {
            var c = FileList.ItemContainerGenerator.ContainerFromIndex(Rows.IndexOf(r)) as ListBoxItem;
            c?.Classes.Remove("drag-hover");
        }

        if (hit?.DataContext is ExplorerRow targetRow)
        {
            var c = FileList.ItemContainerGenerator.ContainerFromIndex(Rows.IndexOf(targetRow)) as ListBoxItem;
            c?.Classes.Add("drag-hover");
        }

    }

    private void OnRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging || _dragRow == null)
            return;

        _isDragging = false;

        DragGhost.IsVisible = false;

        var pos = e.GetPosition(FileList);
        var hit = FileList.InputHitTest(pos) as Control;
        if (hit?.DataContext is not ExplorerRow targetRow)
        {
            _dragRow = null;
            return;
        }

        // Simple rule: if target is a directory, move dragged item into it
        if (!targetRow.Item.IsDirectory)
        {
            _dragRow = null;
            return;
        }

        var draggedItem = _dragRow.Item;
        var targetFolder = targetRow.Item;

        if (draggedItem.FullPath == targetFolder.FullPath)
        {
            _dragRow = null;
            return;
        }

        string source = draggedItem.FullPath;
        string dest = Path.Combine(targetFolder.FullPath, draggedItem.Name);

        try
        {
            if (draggedItem.IsDirectory)
                Directory.Move(source, dest);
            else
                File.Move(source, dest);

            FileMoved?.Invoke(source, dest);
            ProjectDirty?.Invoke();
            Refresh();
        }
        catch (Exception ex)
        {
            Log.QuickLog("Custom drag-drop move failed: " + ex.Message);
        }

        foreach (var r in Rows)
        {
            var c = FileList.ItemContainerGenerator.ContainerFromIndex(Rows.IndexOf(r)) as ListBoxItem;
            if (c != null)
            {
                c.Classes.Remove("drag-hover");
                c.Classes.Remove("dragging-self");
            }
        }

        _dragRow = null;
    }

    private void OnRowDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control c || c.DataContext is not ExplorerRow row)
            return;

        var item = row.Item;

        if (item.IsDirectory)
        {
            // Expand/collapse on double‑click
            item.IsExpanded = !item.IsExpanded;
            RefreshRows();
            FolderOpened?.Invoke(item.FullPath);
            return;
        }

        // It's a file → open it
        FileOpened?.Invoke(item.FullPath);
    }

    // ------------------------------------------------------------
    // Navigation
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
    // File operations (same logic as before)
    // ------------------------------------------------------------

    private string GetCreationDirectory()
    {
        if (FileList.SelectedItem is not ExplorerRow row)
            return _currentDir;

        var item = row.Item;

        if (item.IsDirectory)
            return item.FullPath;

        return Path.GetDirectoryName(item.FullPath)!;
    }

    private string GetSafePath(string folder, string name, bool isFolder)
    {
        string baseName = Path.GetFileNameWithoutExtension(name);
        string ext = Path.GetExtension(name);

        string path = Path.Combine(folder, name);
        int i = 1;

        while (Directory.Exists(path) || File.Exists(path))
        {
            string newName = isFolder
                ? $"{baseName} ({i})"
                : $"{baseName} ({i}){ext}";

            path = Path.Combine(folder, newName);
            i++;
        }

        return path;
    }

    private async void OnNewFile(object? sender, RoutedEventArgs e)
    {
        if (_forcedRoot == null)
            return;

        var dialog = new NewFileDialog();
        var window = MainWindow.window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        string folder = GetCreationDirectory();
        string safePath = GetSafePath(folder, name, isFolder: false);

        File.WriteAllText(safePath, "");
        ProjectDirty?.Invoke();
        Refresh();
        SelectPath(safePath);
    }

    private async void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        if (_forcedRoot == null)
            return;

        var dialog = new InputDialog("New Folder", "Enter folder name:");
        var window = MainWindow.window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        string folder = GetCreationDirectory();
        string safePath = GetSafePath(folder, name, isFolder: true);

        Directory.CreateDirectory(safePath);
        ProjectDirty?.Invoke();
        Refresh();
        SelectPath(safePath);
    }

    private void OnCopy(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerRow row)
            return;

        ClipboardPath = row.Item.FullPath;
        _clipboardIsCut = false;
    }

    private void OnCut(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerRow row)
            return;

        ClipboardPath = row.Item.FullPath;
        _clipboardIsCut = true;
    }

    private async void OnPaste(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ClipboardPath))
            return;

        // Determine where to paste
        string targetFolder = GetCreationDirectory();

        // Determine safe destination path
        string name = Path.GetFileName(ClipboardPath);
        bool isFolder = Directory.Exists(ClipboardPath);
        string safePath = GetSafePath(targetFolder, name, isFolder);

        try
        {
            if (_clipboardIsCut)
            {
                // CUT = Move
                if (isFolder)
                    Directory.Move(ClipboardPath, safePath);
                else
                    File.Move(ClipboardPath, safePath);
            }
            else
            {
                // COPY = Duplicate
                if (isFolder)
                    CopyDirectory(ClipboardPath, safePath);
                else
                    File.Copy(ClipboardPath, safePath);
            }

            ProjectDirty?.Invoke();

            // Expand the folder we pasted into
            var targetItem = FindItemByPath(targetFolder);
            if (targetItem != null)
                targetItem.IsExpanded = true;
            Refresh();
        }
        catch
        {
            var window = MainWindow.window;
            await new PopupWindowDialog("Error", "Could not paste item", "").ShowAsync(window);
        }

        // If it was a cut, clear clipboard
        if (_clipboardIsCut)
        {
            ClipboardPath = "";
            _clipboardIsCut = false;
        }
    }

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerRow row)
            return;

        var item = row.Item;

        var dialog = new DeletionConfirmationDialog();
        var window = MainWindow.window;
        var doDelete = await dialog.ShowAsync(window);

        if (doDelete == "delete")
        {
            if (item.IsDirectory)
                Directory.Delete(item.FullPath, true);
            else
                File.Delete(item.FullPath);

            FileRemoved?.Invoke(item.FullPath);
            ProjectDirty?.Invoke();
            Refresh();
        }
    }

    private async void OnRename(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerRow row)
            return;

        var item = row.Item;

        var dialog = new InputDialog("Rename", $"Enter new name for \"{item.Name}\":");
        var window = MainWindow.window;
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
        if (FileList.SelectedItem is not ExplorerRow row)
            return;

        var item = row.Item;

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
        var window = MainWindow.window;
        var result = await dialog.ShowAsync(window);

        var targetDir = GetCreationDirectory();

        if (result != null)
        {
            foreach (var file in result)
            {
                string destPath = Path.Combine(targetDir, Path.GetFileName(file));
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
        if (FileList.SelectedItem is not ExplorerRow row)
            return;

        var item = row.Item;

        var dialog = new SaveFileDialog { InitialFileName = item.Name };
        var window = MainWindow.window;
        var result = await dialog.ShowAsync(window);

        if (!string.IsNullOrEmpty(result))
        {
            if (item.IsDirectory)
                CopyDirectory(item.FullPath, result);
            else
                File.Copy(item.FullPath, result);
        }
    }

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