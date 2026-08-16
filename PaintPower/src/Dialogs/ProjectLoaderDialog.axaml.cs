using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaintPower;

public partial class ProjectLoaderDialog : Window
{
    private string _currentPath = "";
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();

    private string? _selectedPath;

    private static readonly string[] Extensions =
    {
        ".xPaint", ".xpaint", ".xpa"
    };

    public ProjectLoaderDialog()
    {
        InitializeComponent();

        _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        LoadDirectory(_currentPath);
    }

    // ---------------------------
    // Directory Loading
    // ---------------------------
    private void LoadDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        PathBox.Text = path;
        _currentPath = path;

        var items = new List<FileItem>();

        // Folders
        foreach (var dir in Directory.GetDirectories(path))
        {
            items.Add(new FileItem
            {
                Name = Path.GetFileName(dir) + "/",
                FullPath = dir,
                IsDirectory = true,
            });
        }

        // Files
        foreach (var file in Directory.GetFiles(path))
        {
            var ext = Path.GetExtension(file);
            if(!Extensions.Contains(ext)) continue;

            items.Add(new FileItem
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                IsDirectory = false,
            });
        }

        FileList.ItemsSource = items;
    }

    // ---------------------------
    // Navigation
    // ---------------------------
    private void OnBack(object? sender, RoutedEventArgs e)
    {
        if (_backStack.Count == 0)
            return;

        _forwardStack.Push(_currentPath);
        LoadDirectory(_backStack.Pop());
    }

    private void OnForward(object? sender, RoutedEventArgs e)
    {
        if (_forwardStack.Count == 0)
            return;

        _backStack.Push(_currentPath);
        LoadDirectory(_forwardStack.Pop());
    }

    private void OnUp(object? sender, RoutedEventArgs e)
    {
        var parent = Directory.GetParent(_currentPath);
        if (parent == null)
            return;

        _backStack.Push(_currentPath);
        LoadDirectory(parent.FullName);
    }

    private void OnGo(object? sender, RoutedEventArgs e)
    {
        NavigateTo(PathBox.Text);
    }

    private void OnPathBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            NavigateTo(PathBox.Text);
    }

    private void NavigateTo(string path)
    {
        if (!Directory.Exists(path))
            return;

        _backStack.Push(_currentPath);
        _forwardStack.Clear();
        LoadDirectory(path);
    }

    // ---------------------------
    // Selection + Preview
    // ---------------------------
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FileList.SelectedItem is not FileItem item)
        {
            PreviewName.Text = "Name: -";
            PreviewPath.Text = "Path: -";
            PreviewModified.Text = "Modified: -";
            _selectedPath = null;
            return;
        }

        _selectedPath = item.FullPath;

        PreviewName.Text = $"Name: {item.Name}";
        PreviewPath.Text = $"Path: {item.FullPath}";
        PreviewModified.Text = item.IsDirectory
            ? "Modified: -"
            : $"Modified: {File.GetLastWriteTime(item.FullPath)}";
    }

    // ---------------------------
    // Double-click
    // ---------------------------
    private void OnItemDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (FileList.SelectedItem is not FileItem item)
            return;

        if (item.IsDirectory)
        {
            _backStack.Push(_currentPath);
            LoadDirectory(item.FullPath);
        }
        else if (Extensions.Contains(Path.GetExtension(item.FullPath)))
        {
            Close(item.FullPath);
        }
    }

    // ---------------------------
    // Buttons
    // ---------------------------
    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnOpen(object? sender, RoutedEventArgs e)
    {
        Close(_selectedPath);
    }

    // ---------------------------
    // Static Show
    // ---------------------------
    public static async Task<string?> ShowAsync(Window parent)
    {
        var dlg = new ProjectLoaderDialog();
        return await dlg.ShowDialog<string?>(parent);
    }
}

// ---------------------------
// File Item Model
// ---------------------------
public class FileItem
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public string Icon { get; set; } = "";
}

// Used by PaintProject.cs don't remove!
public class ProjectLoaderResult
{
    public ProjectLoaderMode Mode { get; set; }
    public string Path { get; set; } = "";
}

public enum ProjectLoaderMode
{
    New,
    Open
}