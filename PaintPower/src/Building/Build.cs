using System;
using System.IO;
using System.Threading.Tasks;
using PaintPower.ProjectSystem;
using PaintPower.Templates.FileTemplates;
using Toolbox.Compiler;
using Toolbox.Logging;
using Toolbox.Sessions;

namespace PaintPower.Building;

public class Builder
{

	public string key => session.SessionKey;
	private readonly BuildSession session;

	public bool IsOld { get; private set; } = false;

	private Action<int, int> OnProgress;

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
			count += Directory.GetFiles(sprite.SpriteFolder, "*", SearchOption.AllDirectories).Length;

		return count;
	}

	public async Task<string> BuildProject(PaintProject? project, Action<string, int, int>? onProgress = null)
	{
		if (IsOld) throw new Exception("Build session is expired!");
		IsOld = true;

		string path = $"{session.BuildPath}/{DateTime.Now}/";
		string outputPath = $"{path}build.xpe";

		message = "";
		processed = 0;
		total = 0;

		if (project == null) throw new Exception("Failed to build! No Project!");

		Log.QuickLog("Building...");

		total = CountProjectAssets(project);

		// Compile
		message = "Compiling...";

		foreach (var Sprite in project.Sprites)
			CompileSprite(Sprite, onProgress);
		
		return path;
	}

	private void CompileScript(string path, PaintSprite sprite, Action<string, int, int>? onProgress)
	{
		try {
			WasmCompilerHost.Compile(sprite.Name, sprite.InstanceName, path, key);
		} catch
		{
			Log.QuickLog("It worked.");
			Log.QuickLog(File.ReadAllText(path));
		}

		onProgress?.Invoke(message, processed, total);
	}

	private void CompileSprite(PaintSprite sprite, Action<string, int, int>? onProgress)
	{
		string[] scripts = Directory.GetFiles(sprite.SpriteFolder, "*.pxs", SearchOption.AllDirectories);
		message += $" Sprite({sprite.Name}|{sprite.InstanceName ?? "{No instance name set!}"})";

		processed++;

		foreach (var script in scripts)
		{
			message += $" Compiling script: {script}";
			CompileScript(script, sprite, onProgress);
		}
	}

	private void LinkProgram() {}
}