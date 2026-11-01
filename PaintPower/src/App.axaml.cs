using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace PaintPower;

public partial class App : Application
{

	public Task StartupTask { get; private set; }
	public Task FirstStartupTask { get; private set; }

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{

			// First time launch task setup.
			FirstStartupTask = new Task(() =>
			{
				// Perform first-time launch tasks here
			});

			// Normal setup task.
			StartupTask = new Task(() =>
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
			});
		}

		// Start the tasks
		StartupTask.Start();

		base.OnFrameworkInitializationCompleted();
	}

}