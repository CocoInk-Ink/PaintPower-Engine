using System;
using System.IO;
using System.Threading.Tasks;
using PaintPower.ProjectSystem;
using PaintPower.Templates.FileTemplates;
using Toolbox;
using Toolbox.Accessibility.Translation;
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
			count += Directory.GetFiles(sprite.SpriteFolder, "*.wxa", SearchOption.AllDirectories).Length;

		return count;
	}

	public async Task<string> BuildProject(PaintProject? project, Action<string, int, int>? onProgress = null)
	{
		if (IsOld) throw new Exception("Build session is expired!");
		IsOld = true;

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

		return path;
	}

		private void CompileSprite(PaintSprite sprite, Action<string, int, int>? onProgress)
	{
		string[] scripts = Directory.GetFiles(sprite.SpriteFolder, "*.pxs", SearchOption.AllDirectories);
		message += $" Sprite({sprite.Name}|{sprite.InstanceName ?? "{No instance name set!}"})";

		Log.QuickLog($"Compiling: {sprite}");

		foreach (var script in scripts)
		{
			processed++;
			message += $" Compiling script: {StringTools.GetFilenameFromPath(script)}";
			CompileScript(script, sprite, onProgress);
		}
	}

	private void CompileScript(string path, PaintSprite sprite, Action<string, int, int>? onProgress)
	{
		try
		{
			WasmCompilerHost.Compile(sprite.Name, sprite.InstanceName, path, key);
		}
		catch
		{
			Log.QuickLog("It worked.");
			Log.QuickLog(File.ReadAllText(path));
		}

		Log.QuickLog(StringTools.GetFilenameFromPath(path));
		Log.QuickLog($"{Translator.Map("Building Project")}... Project {(int)processed / total * 100}% built. {message}... ");

		if (processed % 10 == 0) onProgress?.Invoke(message, processed, total);
	}

	private void LinkProgram() { }
}