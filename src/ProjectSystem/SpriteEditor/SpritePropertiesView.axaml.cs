using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PaintPower.ProjectSystem;
using PaintPower.Sprites;
using System;
using System.IO;
using System.Linq;

namespace PaintPower.ProjectSystem.SpriteEditor
{
    public partial class SpritePropertiesView : UserControl
    {
        private PaintSprite? _sprite;
        private bool _suppressSelection = false;

        // Notify ProjectEditorLogic
        public event Action<PaintSprite, SkinDefinition>? SkinSelected;

        public SpritePropertiesView()
        {
            InitializeComponent();

            AddSkinButton.Click += OnAddSkin;
            RemoveSkinButton.Click += OnRemoveSkin;
            RenameSkinButton.Click += OnRenameSkin;
            NameBox.LostFocus += OnNameChanged;
            SkinsList.SelectionChanged += OnSkinSelected;
        }

        // ---------------------------------------------------------
        // Load Sprite
        // ---------------------------------------------------------
        public void LoadSprite(PaintSprite sprite)
        {
            _sprite = sprite;

            NameBox.Text = sprite.Name;

            _suppressSelection = true;
            SkinsList.ItemsSource = sprite.Skins.ToList();
            SkinsList.SelectedItem = null;
            _suppressSelection = false;

            LoadThumbnail();
        }

        private void LoadThumbnail()
        {
            if (_sprite == null)
            {
                ThumbnailImage.Source = null;
                return;
            }

            try
            {
                if (File.Exists(_sprite.ThumbnailPath) &&
                    new FileInfo(_sprite.ThumbnailPath).Length > 0)
                {
                    ThumbnailImage.Source = new Bitmap(_sprite.ThumbnailPath);
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

        // ---------------------------------------------------------
        // Refresh List
        // ---------------------------------------------------------
        private void RefreshList()
        {
            if (_sprite == null) return;

            _suppressSelection = true;
            SkinsList.ItemsSource = _sprite.Skins.ToList();
            SkinsList.SelectedItem = null;
            _suppressSelection = false;
        }

        // ---------------------------------------------------------
        // Rename Sprite
        // ---------------------------------------------------------
        private void OnNameChanged(object? sender, EventArgs e)
        {
            if (_sprite == null) return;

            _sprite.Name = NameBox.Text ?? "";
            RefreshList();
        }

        // ---------------------------------------------------------
        // Add Skin
        // ---------------------------------------------------------
        private void OnAddSkin(object? sender, EventArgs e)
        {
            if (_sprite == null) return;

            string baseName = "New Skin";
            string name = baseName;
            int counter = 1;

            while (_sprite.Skins.Any(s => s.Name == name))
            {
                name = $"{baseName} {counter}";
                counter++;
            }

            _sprite.Skins.Add(new SkinDefinition
            {
                Name = name,
                ScriptPath = "",
                Elements = new()
            });

            _sprite.SaveSkins();
            RefreshList();
        }

        // ---------------------------------------------------------
        // Remove Skin
        // ---------------------------------------------------------
        private void OnRemoveSkin(object? sender, EventArgs e)
        {
            if (_sprite == null) return;
            if (SkinsList.SelectedItem is not SkinDefinition skin) return;

            _sprite.Skins.Remove(skin);
            _sprite.SaveSkins();

            RefreshList();
        }

        // ---------------------------------------------------------
        // Rename Skin
        // ---------------------------------------------------------
        private void OnRenameSkin(object? sender, EventArgs e)
        {
            if (_sprite == null) return;
            if (SkinsList.SelectedItem is not SkinDefinition skin) return;

            skin.Name = skin.Name + " Renamed";
            _sprite.SaveSkins();

            RefreshList();
        }

        // ---------------------------------------------------------
        // Skin Selected → Notify Logic + Generate Thumbnail
        // ---------------------------------------------------------
        private void OnSkinSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection) return;
            if (_sprite == null) return;
            if (SkinsList.SelectedItem is not SkinDefinition skin) return;

            GenerateThumbnailFromSkin(skin);

            // Notify ProjectEditorLogic
            SkinSelected?.Invoke(_sprite, skin);
        }

        // ---------------------------------------------------------
        // Thumbnail Generation
        // ---------------------------------------------------------
        private void GenerateThumbnailFromSkin(SkinDefinition skin)
        {
            if (_sprite == null) return;

            var runtimeSprite = _sprite.ToRuntimeSprite();
            int index = _sprite.Skins.IndexOf(skin);
            if (index < 0) return;

            runtimeSprite.SetSkin(index);
            runtimeSprite.SnapshotDirty = true;
            runtimeSprite.RenderSnapshot();

            var g = runtimeSprite.SnapshotGraphic;

            try
            {
                int stride = g.Width * 4;

                unsafe
                {
                    fixed (byte* ptr = g.Pixels)
                    {
                        using var bmp = new Bitmap(
                            Avalonia.Platform.PixelFormat.Bgra8888,
                            Avalonia.Platform.AlphaFormat.Premul,
                            (IntPtr)ptr,
                            new Avalonia.PixelSize(g.Width, g.Height),
                            new Avalonia.Vector(96, 96),
                            stride);

                        using var fs = File.Open(_sprite.ThumbnailPath, FileMode.Create);
                        bmp.Save(fs);
                    }
                }

                ThumbnailImage.Source = new Bitmap(_sprite.ThumbnailPath);
            }
            catch
            {
                ThumbnailImage.Source = null;
            }
        }
    }
}
