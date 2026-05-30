using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PaintPower.ProjectSystem;
using System;
using System.IO;
using System.Linq;

namespace PaintPower.SpriteEditor;

public partial class SpritePropertiesView : UserControl
{
    private PaintSprite? _sprite;

    // Prevent SelectionChanged from firing during list refresh
    private bool _suppressSelection = false;

    public SpritePropertiesView()
    {
        InitializeComponent();

        AddSkinButton.Click += OnAddSkin;
        RemoveSkinButton.Click += OnRemoveSkin;
        RenameSkinButton.Click += OnRenameSkin;
        NameBox.LostFocus += OnNameChanged;
        SkinsList.SelectionChanged += OnSkinSelected;
    }

    public void LoadSprite(PaintSprite sprite)
    {
        _sprite = sprite;

        NameBox.Text = sprite.Name;

        _suppressSelection = true;
        SkinsList.ItemsSource = null;
        SkinsList.ItemsSource = sprite.Skins;
        SkinsList.SelectedItem = null;
        _suppressSelection = false;

        // Thumbnail
        try
        {
            if (File.Exists(sprite.ThumbnailPath) &&
                new FileInfo(sprite.ThumbnailPath).Length > 0)
            {
                ThumbnailImage.Source = new Bitmap(sprite.ThumbnailPath);
            }
            else
            {
                ThumbnailImage.Source = null;
            }
        }
        catch
        {
            ThumbnailImage.Source = null;
        }
    }

    private void RefreshList()
    {
        if (_sprite == null) return;

        _suppressSelection = true;

        // Create a NEW list instance so Avalonia refreshes safely
        SkinsList.ItemsSource = _sprite.Skins.ToList();

        // Clear selection AFTER assigning ItemsSource
        SkinsList.SelectedItem = null;

        _suppressSelection = false;
    }

    private void OnNameChanged(object? sender, EventArgs e)
    {
        if (_sprite == null) return;

        _sprite.Name = NameBox.Text ?? "";
        RefreshList();
    }

    private async void OnAddSkin(object? sender, EventArgs e)
    {
        if (_sprite == null) return;

        var window = this.VisualRoot as Window;
        if (window == null) return;

        // Open custom file chooser
        var dialog = new ChooseSpriteFileDialog(_sprite.ItemsFolder);
        var chosen = await dialog.ShowDialogAsync(window);

        if (string.IsNullOrWhiteSpace(chosen))
            return;

        // Add skin
        _sprite.Skins.Add(new SkinDefinition
        {
            Name = Path.GetFileNameWithoutExtension(chosen),
            File = $"items/{chosen.Replace("\\", "/")}"
        });

        _sprite.SaveSkins();
        RefreshList();
    }

    private void OnRemoveSkin(object? sender, EventArgs e)
    {
        if (_sprite == null) return;
        if (SkinsList.SelectedItem is not SkinDefinition skin) return;

        _sprite.Skins.Remove(skin);
        _sprite.SaveSkins();

        RefreshList();
    }

    private void OnRenameSkin(object? sender, EventArgs e)
    {
        if (_sprite == null) return;
        if (SkinsList.SelectedItem is not SkinDefinition skin) return;

        skin.Name = skin.Name + " Renamed";
        _sprite.SaveSkins();

        RefreshList();
    }

    private void OnSkinSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection) return; // <-- prevents Avalonia crash
        if (_sprite == null) return;
        if (SkinsList.SelectedItem is not SkinDefinition skin) return;

        GenerateThumbnailFromSkin(skin);
    }

    private void GenerateThumbnailFromSkin(SkinDefinition skin)
    {
        if (_sprite == null) return;

        string fullPath = Path.Combine(_sprite.SpriteFolder, skin.File);

        if (!File.Exists(fullPath))
        {
            ThumbnailImage.Source = null;
            return;
        }

        try
        {
            var bmp = new Bitmap(fullPath);

            using (var fs = File.OpenWrite(_sprite.ThumbnailPath))
                bmp.Save(fs);

            ThumbnailImage.Source = bmp;
        }
        catch
        {
            ThumbnailImage.Source = null;
        }
    }
}
