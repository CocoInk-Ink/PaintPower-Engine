using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PaintPower.Dialogs;
using Toolbox.SoundEffects;

namespace PaintPower.Editors;

public partial class HomeView : Editor
{
    public HomeView()
    {
        InitializeComponent();
    }

    private async void OnNewProject(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        await MainWindow.window.mainGui.NewProject();
    }

    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();

        var window = MainWindow.window;
        if (window == null) return;

        var path = await ProjectLoaderDialog.ShowAsync(window);
        if (!string.IsNullOrWhiteSpace(path))
            await MainWindow.window.mainGui.OpenProject(path);
    }

    private async void OnOpenFolder(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();

        var window = MainWindow.window;
        if (window == null) return;

        var folder = await window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Open Folder" });

        if (folder.Count > 0)
            await MainWindow.window.mainGui.OpenFolder(folder[0].Path.LocalPath);
    }

    private async void OnNewFile(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();
        await MainWindow.window.mainGui.NewSingleFile();
    }

    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        SoundEffects.Click.Play();

        var window = MainWindow.window;
        if (window == null) return;

        var file = await window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "Open File", AllowMultiple = false });

        if (file.Count > 0)
            await MainWindow.window.mainGui.OpenSingleFile(file[0].Path.LocalPath);
    }
}
