using System;
using System;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jint;
using PaintScript_Engine.Versions;
using Toolbox.Logging;
using Toolbox.Plumbing;
using Toolbox.Sessions;
using Toolbox.Time;

namespace Toolbox.Runtime;

public class PaintScriptRuntime
{
	private PaintScriptEngine_Alpha_0_1_0.PaintScriptEngine? paintScriptEngine;
	private bool isSetup = false;
	private dynamic? program;

	public void Setup(string path)
	{
		try
		{

			string json;

			try
			{
				// Load the JSON IR (replace with your actual file path or JSON string)
				json = File.ReadAllText(path);
			}
			catch
			{
				return;
			}

			// Deserialize into PSProgram
			program = JsonSerializer.Deserialize<PaintScriptEngine_Alpha_0_1_0.PSProgram>(
				json,
				new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				}
			);

			if (program == null)
			{
				Console.WriteLine("Failed to load PaintScript program.");
				return;
			}

			// Create the engine
			paintScriptEngine = new PaintScriptEngine_Alpha_0_1_0.PaintScriptEngine(program);
		}
		catch { }
	}

	public async void Start()
	{
		if (paintScriptEngine == null || program == null) return;

		// Tick loop
		Console.WriteLine("Running PaintScript program...");

		// Start @Start event on all targets
		foreach (var target in program.Targets)
		{
			paintScriptEngine.StartEvent(target, "Start");
		}

		while (true)
		{
			await paintScriptEngine?.TickAsync();
			await Task.Delay(10); // small delay to avoid CPU burn
		}
	}

	public async void ForceEvent(string EventName)
	{
		if (paintScriptEngine == null || program == null) return;
		foreach (var target in program)
		{
			paintScriptEngine.StartEvent(target, EventName);
		}
	}

	public async void ForceMessage(string EventName)
	{
		if (paintScriptEngine == null || program == null) return;
		foreach (var target in program)
		{
			// Nothing to add here...
		}
	}
}

public class RuntimeState
{
	public class Targets { }
	public Targets[] targets;
	public bool RefreshNeeded;
	public string[] NewLogs;
}

public class PaintProjectRuntime
{
	private static Dictionary<string, Dictionary<string, string>> Runtimes = null!;
	private readonly Engine engine;

	public PaintProjectRuntime(string runtimeVersion = "0.1", BuildSession? selectedBuild = null)
	{
		engine = new Engine();

		Runtimes = new()
		{
			["0.1"] = new()
			{
				// Latest of this minor version
				["latest"] = "0",
				["0"] = ResourceKit.Other.Paths.Runtimes.r0_1_0
			}
		};

		string SelectedVersionPath = Runtimes[runtimeVersion][Runtimes[runtimeVersion]["latest"]];

		if (selectedBuild == null)
		{
			selectedBuild = new(Session.Current);

		}

		var code = File.ReadAllText(Path.Combine(selectedBuild.BuildPath, "scripts", "linked.json"));

		// Load runtime.js
		var runtimeJs = File.ReadAllText(SelectedVersionPath);
		engine.Execute(runtimeJs);

		engine.Invoke("f", code);
		engine.Invoke("setup");
	}

	public void Start()
	{
		engine.Invoke("Start");
	}

	public RuntimeState Tick()
	{
		engine.Invoke("Tick");

		var result = engine.Invoke("").ToString();
		var json = JsonSerializer.Deserialize<RuntimeState>(result);

		if (json == null) throw new Exception("Invalid null return!");

		return json;
	}
}