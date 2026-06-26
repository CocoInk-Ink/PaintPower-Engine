using Avalonia.Data.Converters;
using PaintPower.FileExplorer;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PaintPower.Tools.Converters;

public class DepthToGuidesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is ExplorerRow row)
        {
            var guides = new List<bool>();
            for (int i = 0; i < row.Depth; i++)
                guides.Add(true);
            return guides;
        }

        return Array.Empty<bool>();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
