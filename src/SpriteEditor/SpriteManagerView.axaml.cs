using Avalonia.Controls;
using Avalonia.Interactivity;
using PaintPower.Accessibility.Translation;
using PaintPower.ProjectSystem;
using System;
using System.Collections.ObjectModel;
using System.IO;
using PaintPower.Dialogs;
using System.Threading.Tasks;

namespace PaintPower.SpriteEditor;

public partial class SpriteManagerView : UserControl
{
    public ObservableCollection<PaintSprite> Sprites { get; } = new();

    private PaintProject _project;

    public event Action<PaintSprite>? SpriteSelected;

    public SpriteManagerView()
    {
        InitializeComponent();
        SpriteList.ItemsSource = Sprites;
    }

    private string translate(string s)
    {
        return Translator.Map(s);
    }

    public void TranslateGUI()
    {
        DeleteSpriteButton.Content = translate("Delete");
        ExportSpriteButton.Content = translate("Export");
        ImportSpriteButton.Content = translate("Import");
        RenameSpriteButton.Content = translate("Rename");
        DuplicateSpriteButton.Content = translate("Duplicate");
        NewSpriteButton.Content = translate("New Sprite");

        InvalidateVisual();
    }

    public void Initialize(PaintProject project)
    {
        _project = project;
        Sprites.Clear();

        foreach (var sprite in project.Sprites)
            Sprites.Add(sprite);
    }

    private void OnSpriteSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (SpriteList.SelectedItem is PaintSprite sprite)
            SpriteSelected?.Invoke(sprite);
    }

    private void OnSpriteDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (SpriteList.SelectedItem is PaintSprite sprite)
            SpriteSelected?.Invoke(sprite);
    }

    private void OnNewSprite(object? sender, RoutedEventArgs e)
    {
        var name = Translator.Map("Sprite") + " " + (Sprites.Count + 1);

        string spritesDir = Path.Combine(_project.Workspace.ItemsDir, "sprites");
        Directory.CreateDirectory(spritesDir);

        string folder = Path.Combine(spritesDir, name);
        Directory.CreateDirectory(folder);

        // Create default files
        File.WriteAllText(Path.Combine(folder, "Sprite.json"), "{}");
        File.WriteAllText(Path.Combine(folder, "Sprite.pss"), "");
        File.WriteAllBytes(Path.Combine(folder, "Sprite.png"), Array.Empty<byte>());
        File.WriteAllBytes(Path.Combine(folder, "Sprite.wxa"), Array.Empty<byte>());
        Directory.CreateDirectory(Path.Combine(folder, "items"));

        var sprite = new PaintSprite
        {
            Name = name,
            SpriteFolder = folder
        };

        _project.Sprites.Add(sprite);
        Sprites.Add(sprite);
    }

    private void OnImportSprite(object? sender, RoutedEventArgs e)
    {
        if (SpriteList.SelectedItem is PaintSprite sprite)
        {
            // TODO: Implement .pSprite import
        }
    }

    private void OnExportSprite(object? sender, RoutedEventArgs e)
    {
        if (SpriteList.SelectedItem is PaintSprite sprite)
        {
            // TODO: Implement .pSprite export
        }
    }

    private void OnDuplicateSprite(object? sender, RoutedEventArgs e)
    {
        if (SpriteList.SelectedItem is not PaintSprite sprite)
            return;

        // Duplicate the sprite
        var newSprite = PaintSprite.Duplicate(sprite);

        // Add to project + UI
        _project.Sprites.Add(newSprite);
        Sprites.Add(newSprite);
    }

    private async void OnDeleteSprite(object? sender, RoutedEventArgs e)
    {
        if (SpriteList.SelectedItem is PaintSprite sprite)
        {
            var dialog = new DeletionConfirmationDialog();
            var window = this.VisualRoot as Window;
            var doDelete = await dialog.ShowAsync(window);
            if (doDelete == "delete")
            {
                PaintSprite.Delete(sprite);
                Sprites.Remove(sprite);
                _project.Sprites.Remove(sprite);
            }
        }
    }

    private async void OnRenameSprite(object? sender, RoutedEventArgs e)
    {
        if (SpriteList.SelectedItem is not PaintSprite sprite)
            return;

        bool isValid = false;

        do
        {

            var dialog = new InputDialog("Rename sprite", $"Enter new name for \"{sprite.Name}\":");
            var window = this.VisualRoot as Window;
            var name = await dialog.ShowAsync(window);

            if (string.IsNullOrWhiteSpace(name)) {
                isValid = true; // Cancelled, keep old name
                continue;
            }

            string parent = Directory.GetParent(sprite.SpriteFolder)!.FullName;

            string? safeName = PaintSprite.SafeRename(name, parent);

            if (safeName == null)
            {
                var errorDialog = new PopupWindowDialog(translate("Error"), translate("Invalid name"), "Please enter a different name.");
                await errorDialog.ShowAsync(window);
                continue;
            }

            isValid = true;

            PaintSprite.Rename(sprite, safeName);
        } while (isValid == false);

        // Refresh UI
        Sprites.Remove(sprite);
        Sprites.Add(sprite);
    }
}