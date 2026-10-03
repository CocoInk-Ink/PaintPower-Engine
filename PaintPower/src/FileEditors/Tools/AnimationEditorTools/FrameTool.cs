using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;

namespace PaintPower.FileEditors.Tools.AnimationEditorTools;

public class FrameTool : INotifyPropertyChanged
{
    public LayerManagerTool Layers { get; } = new();
    public int Index { get; private set; }

    private Bitmap? _thumbnail;
    private bool _thumbnailDirty = true;
    private double _thumbnailCanvasWidth;
    private double _thumbnailCanvasHeight;

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set
        {
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool HasCurrentThumbnail(double canvasWidth, double canvasHeight) =>
        !_thumbnailDirty &&
        _thumbnail != null &&
        _thumbnailCanvasWidth == canvasWidth &&
        _thumbnailCanvasHeight == canvasHeight;

    public void SetFrame(int index)
    {
        Index = index;
    }

    public void InvalidateThumbnail()
    {
        _thumbnailDirty = true;
    }

    public void UpdateThumbnail(Bitmap bmp, double canvasWidth, double canvasHeight)
    {
        _thumbnailCanvasWidth = canvasWidth;
        _thumbnailCanvasHeight = canvasHeight;
        _thumbnailDirty = false;
        Thumbnail = bmp;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
