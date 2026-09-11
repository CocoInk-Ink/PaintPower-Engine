using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PaintPower.Tools.Converters;

public class FrameSelectedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int selected && parameter is int frameIndex)
        {
            return selected == frameIndex ? Brushes.DodgerBlue : Brushes.Gray;
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
