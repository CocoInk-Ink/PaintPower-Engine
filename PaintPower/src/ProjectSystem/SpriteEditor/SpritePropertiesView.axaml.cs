using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PaintPower.ProjectSystem;
using Toolbox.Display.Sprites;
using System;
using System.IO;
using Toolbox;

namespace PaintPower.ProjectSystem.SpriteEditor
{
    public partial class SpritePropertiesView : TranslatableControl
    {
        private PaintSprite? _sprite;

        public SpritePropertiesView()
        {
            InitializeComponent();

            NameBox.LostFocus += OnNameChanged;
            InstanceNameBox.LostFocus += OnInstanceNameChanged;
        }

        // ---------------------------------------------------------
        // Load Sprite
        // ---------------------------------------------------------
        public void LoadSprite(PaintSprite sprite)
        {
            _sprite = sprite;

            NameBox.Text = sprite.Name;
            InstanceNameBox.Text = sprite.InstanceName ?? "";

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
        // Rename Sprite
        // ---------------------------------------------------------
        private void OnNameChanged(object? sender, EventArgs e)
        {
            if (_sprite == null) return;

            _sprite.Name = NameBox.Text ?? "";
        }
        // ---------------------------------------------------------
        // Rename Instance Name
        // ---------------------------------------------------------
        private void OnInstanceNameChanged(object? sender, EventArgs e)
        {
            if (_sprite == null) return;

            _sprite.InstanceName = InstanceNameBox.Text ?? "";
        }

        // ---------------------------------------------------------
        // Thumbnail Generation
        // ---------------------------------------------------------
        public void ShowSkinThumbnail(SkinDefinition skin)
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
