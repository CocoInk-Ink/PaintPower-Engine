// NullToBoolConverter.cs

using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Assets.Converters;

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool res;

        // Treat UnsetValue as null
        if (value == null || value == AvaloniaProperty.UnsetValue)
            res = false;
        else res = true;

        Console.WriteLine($"Normal {res}");

        return res;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToInvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool res;
        // Treat UnsetValue as null
        if (value == null || value == AvaloniaProperty.UnsetValue)
            res = true;
        else
            res = false;
        
        Console.WriteLine($"Inverted {res}");

        return res;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
