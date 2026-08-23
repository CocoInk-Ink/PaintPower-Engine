using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PaintPower.Theming;

namespace PaintPower;

public class PaintPowerApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // ------------------------------------------------------------
        // 1. Register built‑in themes
        // ------------------------------------------------------------
        ThemeManager.RegisterBuiltInTheme(
            "PaintPower",
            "PaintPowerTheme"
        );

        // ------------------------------------------------------------
        // 2. Apply default theme
        // ------------------------------------------------------------
        //ThemeManager.ApplyTheme("PaintPower");

        // ------------------------------------------------------------
        // 3. Standard Avalonia initialization
        // ------------------------------------------------------------
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create the main window
            var window = new MainWindow();

            // Handle startup file (double‑click / OS association)
            if (desktop.Args is { Length: > 0 })
            {
                string file = desktop.Args[0];
                if (System.IO.File.Exists(file) &&
                    file.EndsWith(".xPaint", System.StringComparison.OrdinalIgnoreCase))
                {
                    window.StartupProjectPath = file;
                }
            }

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
