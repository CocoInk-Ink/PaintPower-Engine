using System;
using Toolbox.Plumbing;
using Toolbox.Sessions;
using Wasmtime;

namespace Toolbox.Building.Compiler;

public static class WasmCompilerHost
{
	private static Dictionary<string, Dictionary<string, string>> Compilers = null!;
	public static string Compile(
		string spriteName,
		string instanceName,
		string scriptPath,
		string sessionId)
	{

		Compilers = new() {
			["0.1"] = new () {
				// Latest of this minor version
				["latest"] = "1",
				//["1"] = ResourceKit.Other.Paths.Compilers.Compiler0_1.c0_1_0
			}
		};

		string compilerVersion = "0.1";

		string SelectedVersionPath = Compilers[compilerVersion][Compilers[compilerVersion]["latest"]];

		var engine = new Engine();
		var module = Module.FromFile(engine, SelectedVersionPath);
		var store = new Store(engine);
		var linker = new Linker(engine);

		linker.DefineWasi();

		store.SetWasiConfiguration(new WasiConfiguration()
			.WithPreopenedDirectory(Session.CurrentBuildSession.BuildPath, "/sessions", true)
			.WithInheritedStandardOutput()
			.WithInheritedStandardError()
		);

		var instance = linker.Instantiate(store, module);

		var compileFunc = instance.GetFunction("compile_script");

		if (compileFunc is null)
			throw new Exception("WASM module does not export a 'compile_script' function.");

		// For now, assume compile(...) takes no args and returns an int or something simple.
		// Later, when we know the exact signature, we’ll wire spriteName, instanceName, etc.
		var result = compileFunc.Invoke(spriteName, instanceName, scriptPath, sessionId);

		return result?.ToString() ?? "";
	}
}
