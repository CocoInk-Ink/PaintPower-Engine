using System;
using Toolbox.Plumbing;
using Wasmtime;

namespace Toolbox.Compiler;

public static class WasmCompilerHost
{
    public static string Compile(
        string spriteName,
        string instanceName,
        string scriptText,
        string sessionId)
    {
		ResourceKit.AssetsPath.
        using var engine = new Engine();
        using var module = Module.FromFile(engine, "compiler.wasm");
        using var store = new Store(engine);
        using var linker = new Linker(engine);

        // If your WASM uses WASI, you’d define it here:
        // linker.DefineWasi();
        // store.SetWasiConfiguration(new WasiConfiguration());

        var instance = linker.Instantiate(store, module);

        // Get the exported compile function
        var compileFunc = instance.GetFunction("compile");
        if (compileFunc is null)
            throw new Exception("WASM module does not export a 'compile' function.");

        // For now, assume compile(...) takes no args and returns an int or something simple.
        // Later, when we know the exact signature, we’ll wire spriteName, instanceName, etc.
        var result = compileFunc.Invoke(store);

        return result?.ToString() ?? "";
    }
}
