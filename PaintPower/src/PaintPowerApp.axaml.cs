using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PaintPower.Theming;
using Toolbox.Logging;
using Toolbox.Sessions;

namespace PaintPower;

public class PaintPowerApp : Application
{

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		Log.QuickLog("Starting Avalonia...");
		Log.QuickLog("Registering themes...");
		ThemeManager.RegisterBuiltInTheme("PaintPower", "PaintPowerTheme");
		Log.QuickLog("Applying themes...");
		ThemeManager.ApplyTheme("PaintPower");

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			if (Session.Current.isFirstRunOrDamaged)
			{
				Log.QuickLog("First run or damaged data detected.");
				var firstLaunchWindow = new FirstLaunchWindow();
				firstLaunchWindow.ProceedRequested += () =>
				{
					Session.Current.Unpack(); // Unpack resources on first run
					
					var mainWindow = CreateMainWindow(desktop.Args);
					desktop.MainWindow = mainWindow;
					mainWindow.Show();
					firstLaunchWindow.Close();
				};
				desktop.MainWindow = firstLaunchWindow;
			}
			else
			{
				Session.Current.Unpack(); // Unpack resources on normal startup
				Log.QuickLog("Normal startup detected.");
				desktop.MainWindow = CreateMainWindow(desktop.Args);
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static MainWindow CreateMainWindow(string[] args)
	{
		var window = new MainWindow();

		if (args.Length > 0 &&
			System.IO.File.Exists(args[0]) &&
			args[0].EndsWith(".xPaint", System.StringComparison.OrdinalIgnoreCase))
		{
			window.StartupProjectPath = args[0];
		}

		return window;
	}
}
