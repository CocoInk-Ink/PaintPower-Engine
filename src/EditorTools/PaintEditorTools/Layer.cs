using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PaintPower.EditorTools.PaintEditorTools;

public class Layer : INotifyPropertyChanged
{
    private string _name;
    private bool _visible = true;
    private double _opacity = 1.0;
    private WriteableBitmap _bitmap;
    private WriteableBitmap _thumbnail;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool Visible
    {
        get => _visible;
        set { _visible = value; OnPropertyChanged(); }
    }

    public double Opacity
    {
        get => _opacity;
        set { _opacity = value; OnPropertyChanged(); }
    }

    public WriteableBitmap Bitmap
    {
        get => _bitmap;
        set { _bitmap = value; OnPropertyChanged(); }
    }

    public WriteableBitmap Thumbnail
    {
        get => _thumbnail;
        set { _thumbnail = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}