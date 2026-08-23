using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using Toolbox.Plumbing;
using Toolbox.Logging;
using System.IO;

namespace PaintPower.Theming;

public static class ThemeManager
{
    private static readonly Dictionary<string, Uri> _themes = new();
    private static ResourceDictionary? _currentTheme;

    public static void RegisterTheme(string name, string uri)
    {
        _themes[name] = new Uri(uri);
    }

    public static void RegisterBuiltInTheme(string name, string? FileName = null)
    {
        RegisterTheme(name, $"avares://Assets/Resources/Themes/{FileName ?? name}.axamlt");
    }

    public static void ApplyTheme(string name)
    {
        try {
        if (!_themes.TryGetValue(name, out var uri))
            throw new ArgumentException($"Theme '{name}' is not registered.");

        var app = Application.Current;
        if (app == null)
            return;

        // Remove old theme
        if (_currentTheme != null)
            app.Resources.MergedDictionaries.Remove(_currentTheme);

        var p = new Plumber();

        if (!p.AssetPipe.AssetExists(uri)) throw new Exception($"Asset does not exist!: {uri}");

        // Load new theme
        AvaloniaXamlLoader.Load(File.ReadAllText(p.AssetPipe.PipeOut(uri)));
        //app.Resources.MergedDictionaries.Add(dict);

       // _currentTheme = dict;
        } catch {}
    }

    public static IEnumerable<string> GetThemes() => _themes.Keys;
}
