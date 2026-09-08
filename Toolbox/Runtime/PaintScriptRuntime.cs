using System;
using System.Text.Json;
using System.Threading.Tasks;
using PaintScript_Engine.Versions;

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
		foreach (var target in program) {
			paintScriptEngine.StartEvent(target, EventName);
		}
	}

	public async void ForceMessage(string EventName)
	{
		if (paintScriptEngine == null || program == null) return;
		foreach (var target in program) {
			// Nothing to add here...
		}
	}
}