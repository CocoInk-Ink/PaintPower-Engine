// ExplorerConverters.cs

using Avalonia.Data.Converters;
using Avalonia;
using System;
using System.Globalization;
using PaintPower.FileExplorer;

namespace PaintPower.Tools.Converters;

public class DepthToIndentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int depth)
            return new Thickness(depth * 16, 0, 0, 0); // 16px per level

        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FolderArrowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ExplorerRow row)
        {
            if (!row.Item.IsDirectory)
                return ""; // files have no arrow

            return row.Item.IsExpanded ? "▼" : "▶";
        }

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FolderFileIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ""; // no icons
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
