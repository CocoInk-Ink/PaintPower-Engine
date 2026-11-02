using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Toolbox.Logging;
using Toolbox.Plumbing;

namespace Toolbox.Sessions;

public class Session
{
	public static Session Current { get; private set; } = null!;
	public static BuildSession CurrentBuildSession = null!;

	public Session(string version, string Core = "PaintPower-Engine")
	{
		this.Version = version;
		this.Core = Core;
	}

	public string Version { get; private set; } = null!;

	// "PaintPower-Engine", "xPaint", "xPaint Dev Edition"
	public string Core { get; private set; } = "PaintPower-Engine";
	public string SessionId { get; private set; } = Guid.NewGuid().ToString();

	public Environment.SpecialFolder env => (OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem)
		? Environment.SpecialFolder.ProgramFiles
		: (OperatingSystem.IsWindows()) ? Environment.SpecialFolder.ProgramFilesX86
		: (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) ? Environment.SpecialFolder.UserProfile
		: throw new Exception("Unsupported Platform!");

	public string PlatformFolder => Environment.GetFolderPath(env);

	public string UserHome => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

	public string SettingsFileDir => Path.Join(AppDir(), "Settings.json");

	public bool isFirstRunOrDamaged => !File.Exists(SettingsFileDir);

	public string AppDataFolder()
	{
		if (isAppPack)
			return Path.Combine(GetAppPackDataDir(), "xPaint");
		else
			return Path.Combine(UserHome, "xPaint");
	}

	// Are we installed with AppPack?
	public bool isAppPack
	{
		get
		{
#if AppPack
				return true;
#else
			return false;
#endif
		}
	}

	public string AppPackInstallDir => Path.Combine(PlatformFolder, "Coco", "AppPack");
	public string AppPackDataDir => GetAppPackDataDir();
	public string AppPackDir => GetAppPackDir();
	public string GetAppPackDir()
	{
		string folderForPlatform;

		var env = (OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem)
		? Environment.SpecialFolder.ProgramFiles
		: (OperatingSystem.IsWindows()) ? Environment.SpecialFolder.ProgramFilesX86
		: (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) ? Environment.SpecialFolder.UserProfile
		: throw new Exception("Unsupported platform for AppPack directory.");

		folderForPlatform = Environment.GetFolderPath(env);

		return Path.Combine(folderForPlatform, "Coco", "AppPackApps");
	}

	public string GetAppPackDataDir()
	{
		return Path.Combine(AppPackDir, "ApplicationSaveFiles");
	}

	public string AppDir()
	{
		return Path.Join(AppDataFolder(), Core, Version);
	}

	public void StartupAppPack()
	{
		if (isAppPack)
		{
			// Check if AppPack is installed
			if (!Directory.Exists(AppPackInstallDir))
				throw new Exception("AppPack is not installed. Install AppPack or use the standalone version.");

			// Ensure the AppPack directory exists
			if (!Directory.Exists(AppPackDir))
				throw new Exception("AppPack directory does not exist. Ensure AppPack is properly installed.");

			// Ensure the AppPack data directory exists
			if (!Directory.Exists(AppPackDataDir))
				Directory.CreateDirectory(AppPackDataDir);
		}
	}

	public void Setup()
	{
		if (isAppPack)
			StartupAppPack();

		if (!Directory.Exists(AppDir()))
			Directory.CreateDirectory(AppDir());

		var plumber = new Plumber();
		plumber.MakeMainPlumber();
		if (ResourceKit.Plumber == null) ResourceKit.Initialize(plumber);
	}

	// Unpack resources on first run
	public void Unpack()
	{
		ResourceKit.OnReadyToLoadResources?.Invoke();
	}

	public string SessionDir => Path.Join(AppDir(), "Sessions", SessionId);
	public string BuildsDir => Path.Join(SessionDir, "Builds");

	public void Heartbeat()
	{
		if (!Directory.Exists(SessionDir))
			Directory.CreateDirectory(SessionDir);

		if (!Directory.Exists(BuildsDir))
			Directory.CreateDirectory(BuildsDir);

		var heartbeatFile = Path.Join(SessionDir, "heartbeat.txt");
		File.WriteAllText(heartbeatFile, DateTime.Now.ToString());
	}

	public BuildSession NewBuildSession()
	{
		return new BuildSession(this);
	}

	public static void ClearAllSessions(string version, string Core = "PaintPower-Engine")
	{
		var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var appDir = Path.Join(userHome, "xPaint", Core, version);
		var sessionsDir = Path.Join(appDir, "Sessions");

		if (Directory.Exists(sessionsDir))
			Directory.Delete(sessionsDir, recursive: true);
	}

	public static void ClearSession(string version, string sessionId, string Core = "PaintPower-Engine")
	{
		var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var appDir = Path.Join(userHome, "xPaint", Core, version);
		var sessionsDir = Path.Join(appDir, "Sessions");
		var sessionDir = Path.Join(sessionsDir, sessionId);

		if (Directory.Exists(sessionDir))
			Directory.Delete(sessionDir, recursive: true);
	}

	public static void ClearAllDeadSessions(string version, string Core = "PaintPower-Engine")
	{
		var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var appDir = Path.Join(userHome, "xPaint", Core, version);
		var sessionsDir = Path.Join(appDir, "Sessions");

		if (!Directory.Exists(sessionsDir))
			return;

		foreach (var sessionDir in Directory.GetDirectories(sessionsDir))
		{
			var heartbeatFile = Path.Join(sessionDir, "heartbeat.txt");
			if (!File.Exists(heartbeatFile))
			{
				Directory.Delete(sessionDir, recursive: true);
				continue;
			}

			var lastHeartbeat = File.GetLastWriteTime(heartbeatFile);
			if ((DateTime.Now - lastHeartbeat).TotalMinutes > 5)
			{
				Directory.Delete(sessionDir, recursive: true);
			}
		}
	}

	public static void InitializeSession<T>(string[] args, string version = "1.0.0", string core = "PaintPower-Engine") where T : Application, new()
	{
		// Clear dead sessions on startup
		ClearAllDeadSessions(version, core);

		Current = new Session(version, core);
		CurrentBuildSession = new BuildSession(Current);
		Current.Setup();

		// New background thread to periodically update heartbeat
		var heartbeatThread = new System.Threading.Thread(() =>
		{
			while (true)
			{
				Current.Heartbeat();
				Thread.Sleep(5000); // Update every minute
			}
		});

		heartbeatThread.IsBackground = true;
		heartbeatThread.Start();

		// Finally, start the Avalonia application
		BuildAvaloniaApp<T>(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static void BuildAvaloniaApp<T>(string[] args) where T : Application, new()
	{
		// if (typeof(T) != typeof(Application)) throw new ArgumentException("T must be a subclass of Avalonia.Application", nameof(T));

		AppBuilder.Configure<T>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.StartWithClassicDesktopLifetime(args);
	}
}

public class BuildSession
{
	public Session parent;

	public BuildSession(Session session)
	{
		parent = session;
		Session.CurrentBuildSession = this;
	}

	public string BuildPath => Path.Join(parent.BuildsDir, SessionKey);

	public string SessionKey = Guid.NewGuid().ToString();
}