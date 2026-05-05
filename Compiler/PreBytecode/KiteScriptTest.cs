// KiteScriptTest.cs

namespace PaintPower.Compiler.PreBytecode;

using System;
using PaintPower.Logging;
using PaintPower.Runtime.Bytecode;
using PaintPower.Vm;

public static class KiteScriptTest
{
    public async static void Run()
    {
        // 1. Load the script text
        string script = "[#call PopularIds.functions.print](\"Hello, World!\");";

        // 2. Compile to bytecode
        Bytecode bytecode = KiteScriptCompiler.Compile(script);

        // 3. Create a VM thread
        var thread = new VmThread();
        thread.LoadBytecode(bytecode);

        // 4. Execute until finished
        Log.QuickLog("Running KiteScript program:");
        while (!thread.isPaused)
        {
            thread.Step().Wait();
        }

        Log.QuickLog("Done.");
    }
}
