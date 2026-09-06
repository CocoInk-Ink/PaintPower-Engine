using System;
using System.IO;

using Avalonia;

namespace Toolbox.Sessions;

public class Session
{
	public static Session Current { get; private set; } = null!;

	public Session(string version, string Core = "PaintPower-Engine")
	{
		this.Version = version;
		this.Core = Core;
	}

	public string Version { get; private set; } = null!;

	// "PaintPower-Engine", "xPaint", "xPaint Dev Edition"
	public string Core { get; private set; } = "PaintPower-Engine";
	public string SessionId { get; private set; } = Guid.NewGuid().ToString();
	public string UserHome => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
	public string AppDir => Path.Join(UserHome, "xPaint", Core, Version);
	public string SessionDir => Path.Join(AppDir, "Sessions", SessionId);

	public void Heartbeat()
	{
		if (!Directory.Exists(SessionDir))
			Directory.CreateDirectory(SessionDir);

		var heartbeatFile = Path.Join(SessionDir, "heartbeat.txt");
		File.WriteAllText(heartbeatFile, DateTime.Now.ToString());
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