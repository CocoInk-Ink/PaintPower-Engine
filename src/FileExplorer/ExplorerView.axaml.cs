using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PaintPower.Accessibility.Translation;
using PaintPower.Dialogs;
using PaintPower.Logging;
using PaintPower.ProjectSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace PaintPower.FileExplorer;

public partial class ExplorerView : UserControl
{
    public ObservableCollection<ExplorerItem> Items { get; } = new();

    private string _currentDir = "";
    private TempWorkspace? _workspace;

    public string ClipboardPath { get; private set; } = "";

    // Events so parent editors can react
    public event Action<string>? FileOpened;
    public event Action<string>? FolderOpened;
    public event Action? ProjectDirty;

    public ExplorerView()
    {
        InitializeComponent();
        Translator.LanguageChanged += () => Refresh();
    }

    // Called by parent editor after workspace is ready
    public void Initialize(TempWorkspace workspace)
    {
        _workspace = workspace;
        _currentDir = workspace.ActiveRoot;

        FileList.ItemsSource = Items;

        Refresh();
    }

    // Switch between different roots (project root, sprite root, etc.)
    public void SetRoot(string newRoot)
    {
        _currentDir = newRoot;
        Refresh();
    }

    private void Refresh()
    {
        TranslateGUI();

        Items.Clear();

        if (_workspace == null || !Directory.Exists(_currentDir))
        {
            PathLabel.Text = "(no project)";
            return;
        }

        string relative = _currentDir.Replace(_workspace.ActiveRoot, "")
                                     .Replace("\\", "/");

        if (string.IsNullOrEmpty(relative))
            relative = "/";

        PathLabel.Text = relative;

        foreach (var dir in Directory.GetDirectories(_currentDir))
        {
            Items.Add(new ExplorerItem
            {
                Name = Path.GetFileName(dir),
                FullPath = dir,
                IsDirectory = true
            });
        }

        foreach (var file in Directory.GetFiles(_currentDir))
        {
            Items.Add(new ExplorerItem
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                IsDirectory = false
            });
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var destFilePath = Path.Combine(destDir, Path.GetFileName(filePath));
            File.Copy(filePath, destFilePath);
        }

        foreach (var directoryPath in Directory.GetDirectories(sourceDir))
        {
            var destDirectoryPath = Path.Combine(destDir, Path.GetFileName(directoryPath));
            CopyDirectory(directoryPath, destDirectoryPath);
        }
    }

    // -----------------------------
    // Navigation
    // -----------------------------
    private void OnGoRoot(object? sender, RoutedEventArgs e)
    {
        if (_workspace == null)
            return;

        _currentDir = _workspace.ActiveRoot;
        Refresh();
    }

    private void OnGoUp(object? sender, RoutedEventArgs e)
    {
        if (_workspace == null)
            return;

        if (_currentDir == _workspace.ActiveRoot)
            return;

        _currentDir = Directory.GetParent(_currentDir)!.FullName;
        Refresh();
    }

    // -----------------------------
    // Create File / Folder
    // -----------------------------
    private async void OnNewFile(object? sender, RoutedEventArgs e)
    {
        if (_workspace == null)
            return;

        var dialog = new NewFileDialog();
        var window = this.VisualRoot as Window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        if (string.IsNullOrEmpty(name))
        {
            var errorDialog = new PopupWindowDialog(
                Translator.Map("Error"),
                Translator.Map("Invalid file name"),
                "Please enter a different name.");
            await errorDialog.ShowAsync(window);
            return;
        }

        string path = Path.Combine(_currentDir, name);

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            File.WriteAllText(path, "");
            ProjectDirty?.Invoke();
        }
        else
        {
            await ShowErrorPopup();
        }

        Refresh();
    }

    private async void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        if (_workspace == null)
            return;

        var dialog = new InputDialog("New Folder", "Enter folder name:");
        var window = this.VisualRoot as Window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        if (string.IsNullOrEmpty(name))
        {
            var errorDialog = new PopupWindowDialog(
                Translator.Map("Error"),
                Translator.Map("Invalid file name"),
                "Please enter a different name.");
            await errorDialog.ShowAsync(window);
            return;
        }

        string path = Path.Combine(_currentDir, name);

        if (!Directory.Exists(path) && !File.Exists(path))
        {
            Directory.CreateDirectory(path);
            ProjectDirty?.Invoke();
        }
        else
        {
            await ShowErrorPopup();
        }

        Refresh();
    }

    // -----------------------------
    // File operations
    // -----------------------------
    private void OnCopy(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerItem item)
            return;

        ClipboardPath = item.FullPath;
    }

    private void OnCut(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerItem item)
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
            else
                throw new Exception("Source path does not exist");

            ProjectDirty?.Invoke();
            Refresh();
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Error pasting file/folder: {ex.Message}");
            var errorDialog = new PopupWindowDialog(
                Translator.Map("Error"),
                Translator.Map("Could not paste item"),
                "Please try again.");
            var window = this.VisualRoot as Window;
            await errorDialog.ShowAsync(window);
        }
    }

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerItem item)
            return;

        var dialog = new DeletionConfirmationDialog();
        var window = this.VisualRoot as Window;
        var doDelete = await dialog.ShowAsync(window);
        if (doDelete == "delete")
        {
            try
            {
                if (item.IsDirectory)
                    Directory.Delete(item.FullPath, true);
                else
                    File.Delete(item.FullPath);

                ProjectDirty?.Invoke();
                Refresh();
            }
            catch (Exception ex)
            {
                Log.QuickLog($"Error deleting file/folder: {ex.Message}");
                var errorDialog = new PopupWindowDialog(
                    Translator.Map("Error"),
                    Translator.Map("Could not delete item"),
                    "Please try again.");
                await errorDialog.ShowAsync(window);
            }
        }
    }

    private async void OnRename(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerItem item)
            return;

        var dialog = new InputDialog("Rename", $"Enter new name for \"{item.Name}\":");
        var window = this.VisualRoot as Window;
        var name = await dialog.ShowAsync(window);

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        if (string.IsNullOrEmpty(name))
        {
            var errorDialog = new PopupWindowDialog(
                Translator.Map("Error"),
                Translator.Map("Invalid name"),
                "Please enter a different name.");
            await errorDialog.ShowAsync(window);
            return;
        }

        string destPath = Path.Combine(_currentDir, name);

        if (File.Exists(destPath) || Directory.Exists(destPath))
        {
            var errorDialog = new PopupWindowDialog(
                Translator.Map("Error"),
                Translator.Map("A file or folder with that name already exists"),
                "Please enter a different name.");
            await errorDialog.ShowAsync(window);
            return;
        }

        try
        {
            if (item.IsDirectory)
                Directory.Move(item.FullPath, destPath);
            else
                File.Move(item.FullPath, destPath);

            ProjectDirty?.Invoke();
            Refresh();
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Error renaming file/folder: {ex.Message}");
            var errorDialog = new PopupWindowDialog(
                Translator.Map("Error"),
                Translator.Map("Could not rename item"),
                "Please try again.");
            await errorDialog.ShowAsync(window);
        }
    }

    private async void OnDuplicate(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerItem item)
            return;

        string nameWithoutExt = Path.GetFileNameWithoutExtension(item.Name);
        string ext = Path.GetExtension(item.Name);
        string newName = $"{nameWithoutExt}_copy{ext}";
        string destPath = Path.Combine(_currentDir, newName);

        int copyIndex = 1;
        while (File.Exists(destPath) || Directory.Exists(destPath))
        {
            newName = $"{nameWithoutExt}_copy{copyIndex}{ext}";
            destPath = Path.Combine(_currentDir, newName);
            copyIndex++;
        }

        try
        {
            if (item.IsDirectory)
                CopyDirectory(item.FullPath, destPath);
            else
                File.Copy(item.FullPath, destPath);

            ProjectDirty?.Invoke();
            Refresh();
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Error duplicating file/folder: {ex.Message}");
            var errorDialog = new PopupWindowDialog(
                Translator.Map("Error"),
                Translator.Map("Could not duplicate item"),
                "Please try again.");
            var window = this.VisualRoot as Window;
            await errorDialog.ShowAsync(window);
        }
    }

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import file",
            AllowMultiple = true
        };

        var window = this.VisualRoot as Window;
        var result = await dialog.ShowAsync(window);
        if (result != null)
        {
            foreach (var file in result)
            {
                string destPath = Path.Combine(_currentDir, Path.GetFileName(file));

                if (File.Exists(destPath) || Directory.Exists(destPath))
                {
                    var errorDialog = new PopupWindowDialog(
                        Translator.Map("Error"),
                        Translator.Map($"A file or folder named \"{Path.GetFileName(file)}\" already exists in this directory"),
                        "Import Error");
                    await errorDialog.ShowAsync(window);
                    continue;
                }

                try
                {
                    File.Copy(file, destPath);
                    ProjectDirty?.Invoke();
                    Refresh();
                }
                catch (Exception ex)
                {
                    Log.QuickLog($"Error importing file: {ex.Message}");
                    var errorDialog = new PopupWindowDialog(
                        Translator.Map("Error"),
                        Translator.Map($"Could not import \"{Path.GetFileName(file)}\""),
                        "Import Error");
                    await errorDialog.ShowAsync(window);
                }
            }
        }
    }

    private async void OnExport(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerItem item)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Export file",
            InitialFileName = item.Name
        };

        var window = this.VisualRoot as Window;
        var result = await dialog.ShowAsync(window);
        if (!string.IsNullOrEmpty(result))
        {
            try
            {
                if (item.IsDirectory)
                    CopyDirectory(item.FullPath, result);
                else
                    File.Copy(item.FullPath, result);
            }
            catch (Exception ex)
            {
                Log.QuickLog($"Error exporting file/folder: {ex.Message}");
                var errorDialog = new PopupWindowDialog(
                    Translator.Map("Error"),
                    Translator.Map("Could not export item"),
                    "Please try again.");
                await errorDialog.ShowAsync(window);
            }
        }
    }

    private async Task ShowErrorPopup()
    {
        var dialog = new PopupWindowDialog(
            "File/Folder Creation Error!",
            "File or folder already exists in this directory!",
            "Error"
        );

        var window = this.VisualRoot as Window;
        try { await dialog.ShowAsync(window); }
        catch { }
    }

    // -----------------------------
    // File selection
    // -----------------------------
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // No action on single click
    }

    private void OnItemDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not ExplorerItem item)
            return;

        if (item.IsDirectory)
        {
            _currentDir = item.FullPath;
            Refresh();
            FolderOpened?.Invoke(item.FullPath);
            return;
        }

        FileOpened?.Invoke(item.FullPath);
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
