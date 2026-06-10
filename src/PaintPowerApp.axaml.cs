using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PaintPower.Theming;

namespace PaintPower;

/// <summary>
/// The main Avalonia application for PaintPower.
/// Wires together:
///   - PaintPowerRuntime (engine logic)
///   - PaintPowerUI (UI glue)
///   - MainWindow (Avalonia window)
///   - ThemeManager (runtime theme loading)
///
/// xPaint can subclass this to override branding, themes, menus, etc.
/// </summary>
public class PaintPowerApp : Application
{
    public PaintPowerRuntime Runtime { get; private set; }
    public PaintPowerUI UI { get; private set; }

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
        ThemeManager.ApplyTheme("PaintPower");

        // ------------------------------------------------------------
        // 3. Standard Avalonia initialization
        // ------------------------------------------------------------
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create runtime
            Runtime = CreateRuntime();

            // Create window
            var window = CreateMainWindow();

            // Attach UI glue
            UI = CreateUI(Runtime, window);

            desktop.MainWindow = window;

            // Handle startup file
            if (desktop.Args is { Length: > 0 })
            {
                string file = desktop.Args[0];
                if (System.IO.File.Exists(file) &&
                    file.EndsWith(".xPaint", System.StringComparison.OrdinalIgnoreCase))
                {
                    window.StartupProjectPath = file;
                }
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    // ------------------------------------------------------------
    // Factory methods — xPaint overrides these
    // ------------------------------------------------------------

    protected virtual PaintPowerRuntime CreateRuntime()
        => new PaintPowerRuntime();

    protected virtual MainWindow CreateMainWindow()
        => new MainWindow();

    protected virtual PaintPowerUI CreateUI(PaintPowerRuntime runtime, MainWindow window)
        => new PaintPowerUI(runtime, window, window.editorPart);
}
