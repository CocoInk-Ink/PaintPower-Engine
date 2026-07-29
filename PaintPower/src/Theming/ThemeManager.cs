using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace PaintPower.Theming;

public static class ThemeManager
{
    private static readonly Dictionary<string, Uri> _themes = new();
    private static ResourceDictionary? _currentTheme;

    public static void RegisterTheme(string name, string uri)
    {
        _themes[name] = new Uri(uri);
    }

    public static void RegisterBuiltInTheme(String name, string FileName = null)
    {
        RegisterTheme(name, $"avares://PaintPower/src/Themes/{FileName ?? name}.axaml");
    }

    public static void ApplyTheme(string name)
    {
        if (!_themes.TryGetValue(name, out var uri))
            throw new ArgumentException($"Theme '{name}' is not registered.");

        var app = Application.Current;
        if (app == null)
            return;

        // Remove old theme
        if (_currentTheme != null)
            app.Resources.MergedDictionaries.Remove(_currentTheme);

        // Load new theme
        var dict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
        app.Resources.MergedDictionaries.Add(dict);

        _currentTheme = dict;
    }

    public static IEnumerable<string> GetThemes() => _themes.Keys;
}
