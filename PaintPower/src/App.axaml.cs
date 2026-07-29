using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace PaintPower;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow();

        // NEW: handle startup file
        if (desktop.Args is { Length: > 0 })
        {
            string file = desktop.Args[0];

            if (File.Exists(file) && file.EndsWith(".xPaint", StringComparison.OrdinalIgnoreCase))
            {
                // Pass file to MainWindow
                ((MainWindow)desktop.MainWindow).StartupProjectPath = file;
            }
        }
    }

    base.OnFrameworkInitializationCompleted();
}

}