using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;

using PaintPower.Accessibility.Translation;

namespace PaintPower.Dialogs;

public partial class ProjectLoaderDialog : Window
{
    public ProjectLoaderDialog() {
        AvaloniaXamlLoader.Load(this);
        Translator.LanguageChanged += TranslateGUI;
    }

    public Task<ProjectLoaderResult?> ShowAsync(Window parent)
    {
        return this.ShowDialog<ProjectLoaderResult?>(parent);
    }

    private async void OnNewProject(object? sender, RoutedEventArgs e)
    {
        var savePicker = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Translator.Map("Create New Project"),
            DefaultExtension = "xPaint",
            SuggestedFileName = $"{Translator.Map("NewProject")}.xPaint",
            ShowOverwritePrompt = true
        });

        if (savePicker != null)
        {
            var r = (new ProjectLoaderResult
            {
                Mode = ProjectLoaderMode.New,
                Path = savePicker.Path.LocalPath
            });
            Close(r);
        } else {
            Close();
        }
    }

    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        TranslateGUI(); // Ensure translation is up to date before opening file dialog, as it may be used in the title or file type descriptions.
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Translator.Map("Open Project"),
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(Translator.Map("PaintPower Project"))
                {
                    Patterns = new[] { "*.xPaint", "*.xpaint", "*.Paint", "*.paint" }
                }
            }
        });

        if (files.Count > 0)
        {
            var r = new ProjectLoaderResult { Mode = ProjectLoaderMode.Open, Path = files[0].Path.LocalPath };
            Close(r);
        }
    }

    public void TranslateGUI()
    {
        WelcomeText.Text = Translator.Map("Welcome to the PaintPower engine!");
        NewProjectButton.Content = Translator.Map("Create New Project");
        OpenProjectButton.Content = Translator.Map("Open Existing Project");

        InvalidateVisual();
    }
}

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