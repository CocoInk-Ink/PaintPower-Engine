using System;
using System.IO;
using System.Threading.Tasks;
using PaintPower.ProjectSystem;
using PaintPower.Templates.FileTemplates;
using Toolbox;
using Toolbox.Accessibility.Translation;
using Toolbox.Building.Compiler;
using Toolbox.Building.Compiler.PaintScriptCompiler;
using Toolbox.Logging;
using Toolbox.Sessions;

namespace PaintPower.Building;

public class Builder
{

	public string key => session.SessionKey;
	private readonly BuildSession session;

	public bool IsOld { get; private set; } = false;

	private Action<int, int>? OnProgress;

	public Builder()
	{
		session = Session.Current.NewBuildSession();
	}

	private string message = "";
	private int total = 0;
	private int processed = 0;

	public int CountProjectAssets(PaintProject project)
	{
		int count = 0;

		foreach (var sprite in project.Sprites)
			count += Directory.GetFiles(sprite.SpriteFolder, "*.wxa", SearchOption.AllDirectories).Length;

		return count;
	}

	public async Task<string> BuildProject(PaintProject? project, Action<string, int, int>? onProgress = null)
	{
		if (IsOld) throw new Exception("Build session is expired!");
		IsOld = true;

		// Preparation for build.
		if (!Directory.Exists(session.BuildPath))
			Directory.CreateDirectory(session.BuildPath);

		if (!Directory.Exists(Path.Combine(session.BuildPath, "scripts")))
			Directory.CreateDirectory(Path.Combine(session.BuildPath, "scripts"));

		// Preparation for compilation.

		string path = Path.Join(session.BuildPath, DateTime.Now.ToString());
		string outputPath = $"{path}build.xpe";

		message = "";
		processed = 0;
		total = 0;

		if (project == null) throw new Exception("Failed to build! No Project!");

		Log.QuickLog("Building...");

		total = CountProjectAssets(project);

		// Compile
		foreach (var Sprite in project.Sprites)
		{
			message = "Compiling...";
			CompileSprite(Sprite, onProgress);
		}

		// Link program

		Log.QuickLog("Compile complete! Linking...");

		LinkProgram();

		Log.QuickLog("Linked successfully!");

		Log.QuickLog("Build was a success!");

		return path;
	}

		private void CompileSprite(PaintSprite sprite, Action<string, int, int>? onProgress)
	{
		string[] scripts = Directory.GetFiles(sprite.SpriteFolder, "*.pxs", SearchOption.AllDirectories);
		message += " " + ((sprite.InstanceName == null || string.IsNullOrWhiteSpace(sprite.InstanceName)) ? sprite.Name : sprite.InstanceName);

		Log.QuickLog($"Compiling: {sprite}");

		string oldMessage = message;

		foreach (var script in scripts)
		{
			processed++;
			message += $" Compiling: {StringTools.GetFilenameFromPath(script)}";
			CompileScript(script, sprite, onProgress);
			message = oldMessage;
		}
	}

	private void CompileScript(string path, PaintSprite sprite, Action<string, int, int>? onProgress)
	{
		try
		{
			PaintScriptCompiler.Compile(sprite.Name, sprite.InstanceName, path, key, session);
			Log.QuickLog($"Successfully compiled {path}");
		}
		catch (Exception e)
		{
			Log.QuickLog($"Build failed!: {e.Message}");
		}

		Log.QuickLog(StringTools.GetFilenameFromPath(path));
		
		onProgress?.Invoke(message, processed, total);
	}

	private void LinkProgram()
	{
		PaintScriptCompiler.Link(session.SessionKey, session.BuildPath);
	}
}