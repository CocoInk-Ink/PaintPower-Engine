using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PaintPower.Tools.Converters;

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Treat UnsetValue as null
        if (value == null || value == AvaloniaProperty.UnsetValue)
            return false;

        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToInvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Treat UnsetValue as null
        if (value == null || value == AvaloniaProperty.UnsetValue)
            return true;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
