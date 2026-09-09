using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Toolbox.Logging;
using Toolbox.Plumbing;

namespace PaintPower.Theming;

public static class ThemeManager
{
    private static readonly Dictionary<string, Uri> _themes = new();
    private static ResourceDictionary? _currentTheme;

    public static void RegisterTheme(string name, string uri)
    {
        _themes[name] = new Uri(uri);
    }

    public static void RegisterBuiltInTheme(string name, string? fileName = null)
    {
        RegisterTheme(name, $"avares://Assets/Resources/Text/Themes/{fileName ?? name}.axamlt");
    }

    public static IEnumerable<string> GetThemes() => _themes.Keys;

    public static void ApplyTheme(string name)
    {
        try
        {
            if (!_themes.TryGetValue(name, out var uri))
                throw new ArgumentException($"Theme '{name}' is not registered.");

            var app = Application.Current;
            if (app == null)
                return;

            // Remove previous theme
            if (_currentTheme != null)
                app.Resources.MergedDictionaries.Remove(_currentTheme);

            var p = Plumber.MainPlumber;

            if (!p.AssetPipe.AssetExists(uri))
                throw new Exception($"Theme asset not found: {uri}");

            // Load raw XML text
            var xmlText = File.ReadAllText(p.AssetPipe.PipeOut(uri));
            Console.WriteLine($"RAW THEME FILE:\n---\n{xmlText}\n---");

            var doc = XDocument.Parse(xmlText);

            var dict = new ResourceDictionary();

            foreach (var el in doc.Root.Elements())
            {
                var key = el.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"))?.Value;
                if (key == null)
                    continue;

                var value = ConvertElement(el, dict);
                if (value != null)
                    dict[key] = value;
            }

            app.Resources.MergedDictionaries.Add(dict);
            _currentTheme = dict;
        }
        catch (Exception ex)
        {
            Log.QuickLog(ex, $"Failed to apply theme '{name}'");
        }
    }

    private static object? ConvertElement(XElement el, ResourceDictionary dict)
    {
        string key = el.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"))?.Value;
        if (key == null)
            return null;

        switch (el.Name.LocalName)
        {
            case "Boolean":
                return bool.Parse(el.Value);

            case "String":
                return el.Value;

            case "Color":
                return Color.Parse(el.Value);

            case "Bitmap":
                {
                    var src = el.Value;

                    if (src.StartsWith("avares://"))
                    {
                        var uri = new Uri(src);
                        return new Bitmap(AssetLoader.Open(uri));
                    }
                    else
                    {
                        return new Bitmap(src);
                    }
                }

            case "SolidColorBrush":
                return ParseSolidColorBrush(el, dict);

            case "ImageBrush":
                return ParseImageBrush(el);

            default:
                return null;
        }
    }

    private static SolidColorBrush ParseSolidColorBrush(XElement el, ResourceDictionary dict)
    {
        var colorAttr = el.Attribute("Color");
        if (colorAttr == null)
            return new SolidColorBrush(Colors.Transparent);

        var colorValue = colorAttr.Value;

        if (colorValue.StartsWith("{StaticResource"))
        {
            var resKey = colorValue.Replace("{StaticResource ", "").Replace("}", "");
            if (dict.TryGetValue(resKey, out var res) && res is Color c)
                return new SolidColorBrush(c);
        }

        return new SolidColorBrush(Color.Parse(colorValue));
    }

    private static ImageBrush ParseImageBrush(XElement el)
    {
        var src = el.Attribute("Source")?.Value;
        if (src == null)
            return new ImageBrush();

        Bitmap bitmap;

        if (src.StartsWith("avares://"))
        {
            var uri = new Uri(src);
            bitmap = new Bitmap(AssetLoader.Open(uri));
        }
        else
        {
            bitmap = new Bitmap(src);
        }

        return new ImageBrush
        {
            Source = bitmap,
            Stretch = Stretch.None,
            TileMode = TileMode.Tile,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            DestinationRect = RelativeRect.Parse("0,0,10,10")
        };
    }
}
