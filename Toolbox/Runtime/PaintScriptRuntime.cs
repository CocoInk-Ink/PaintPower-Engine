using System;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using PaintScript_Engine.Versions;
using Toolbox.Logging;
using Toolbox.Plumbing;
using Toolbox.Sessions;
using Toolbox.Time;
using Toolbox.Display.Sprites;

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

public class Target
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("instance")]
	public string Instance { get; set; } = string.Empty;

	[JsonPropertyName("position")]
	public Position Position { get; set; } = new Position();

	[JsonPropertyName("visible")]
	public bool Visible { get; set; } = true;
}

public class Position
{
	[JsonPropertyName("x")]
	public double X { get; set; }

	[JsonPropertyName("y")]
	public double Y { get; set; }
}

public class RuntimeState
{
	[JsonPropertyName("targets")]
	public Target[] Targets { get; set; } = Array.Empty<Target>();

	[JsonPropertyName("RefreshNeeded")]
	public bool RefreshNeeded { get; set; }

	[JsonPropertyName("NewLogs")]
	public string[] NewLogs { get; set; } = Array.Empty<string>();
}

public class PaintProjectRuntime
{
	private static Dictionary<string, Dictionary<string, string>> Runtimes = null!;
	private readonly Engine engine;

	//						// Instance name to sprite mapping
	private readonly Dictionary<string, Sprite> sprites = new();

	public IReadOnlyDictionary<string, Sprite> Sprites => sprites;

	public PaintProjectRuntime(BuildSession selectedBuild, string runtimeVersion = "0.1")
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

		var linkedProgramPath = Path.Combine(selectedBuild.BuildPath, "scripts", "linked.json");
		var code = File.ReadAllText(linkedProgramPath);
		using (var linkedProgram = JsonDocument.Parse(code))
		{
			if (linkedProgram.RootElement.ValueKind != JsonValueKind.Object
				|| !linkedProgram.RootElement.TryGetProperty("targets", out var targetDefinitions)
				|| targetDefinitions.ValueKind != JsonValueKind.Array)
			{
				throw new InvalidDataException(
					$"Linked PaintScript program '{linkedProgramPath}' must contain a 'targets' array. Rebuild the project and try again.");
			}
		}

		LoadSprites(selectedBuild.BuildPath);

		// Load runtime.js
		var runtimeJs = File.ReadAllText(SelectedVersionPath);
		engine.Execute(runtimeJs);
		engine.Execute(@"
			globalThis.__paintScriptRuntimeInvoke = (name, ...args) => {
				const fn = PaintScriptRuntime[name];
				if (typeof fn !== ""function"") {
					throw new Error(""PaintScript runtime function '"" + name + ""' is not available."");
				}
				const result = fn(...args);
				return name === ""getCurrentState"" ? JSON.stringify(result) : result;
			};
			");

		InvokeRuntime("f", code);
		InvokeRuntime("setup");
	}

	public void Start()
	{
		InvokeRuntime("Start");
	}

	public RuntimeState Tick()
	{
		InvokeRuntime("Tick");

		var result = InvokeRuntime("getCurrentState").ToString();
		var json = JsonSerializer.Deserialize<RuntimeState>(result);

		if (json == null) throw new Exception("Invalid null return!");

		return json;
	}

	private void LoadSprites(string buildPath)
	{
		string manifestPath = Path.Combine(buildPath, "sprites.json");
		if (!File.Exists(manifestPath))
		{
			throw new FileNotFoundException(
				$"Sprite manifest '{manifestPath}' is missing. Rebuild the project to include its sprites.",
				manifestPath);
		}

		using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
		if (manifest.RootElement.ValueKind != JsonValueKind.Array)
			throw new InvalidDataException($"Sprite manifest '{manifestPath}' must contain an array.");

		foreach (JsonElement entry in manifest.RootElement.EnumerateArray())
		{
			string name = GetRequiredString(entry, "name", manifestPath);
			string instanceName = GetRequiredString(entry, "instanceName", manifestPath);
			string folder = GetRequiredString(entry, "folder", manifestPath);
			string spriteFolder = Path.Combine(buildPath, "sprites", folder);

			if (!Directory.Exists(spriteFolder))
				throw new DirectoryNotFoundException(
					$"Sprite folder '{spriteFolder}' from manifest '{manifestPath}' was not found.");

			string spriteKey = string.IsNullOrWhiteSpace(instanceName) ? name : instanceName;
			if (!sprites.TryAdd(spriteKey, Sprite.LoadFromBuildFolder(spriteFolder, name, instanceName)))
				throw new InvalidDataException($"Build contains multiple sprites with instance name '{spriteKey}'.");
		}
	}

	private static string GetRequiredString(JsonElement entry, string propertyName, string manifestPath)
	{
		if (entry.ValueKind != JsonValueKind.Object
			|| !entry.TryGetProperty(propertyName, out JsonElement property)
			|| property.ValueKind != JsonValueKind.String)
		{
			throw new InvalidDataException(
				$"Sprite manifest '{manifestPath}' contains an entry without a string '{propertyName}' property.");
		}

		return property.GetString()!;
	}

	private JsValue InvokeRuntime(string functionName, params object[] arguments)
	{
		var invocationArguments = new object[arguments.Length + 1];
		invocationArguments[0] = functionName;
		Array.Copy(arguments, 0, invocationArguments, 1, arguments.Length);
		return engine.Invoke("__paintScriptRuntimeInvoke", invocationArguments);
	}
}