using System;
using System.IO;
using Jint;
using Toolbox.Plumbing;

namespace Toolbox.Building.Compiler.PaintScriptCompiler;

public class PaintScriptCompiler
{
    private static Dictionary<string, Dictionary<string, string>> Compilers = null!;
    public static string Compile(
        string spriteName,
        string instanceName,
        string scriptPath,
        string sessionId)
    {
        var engine = new Engine();

        Compilers = new()
        {
            ["0.1"] = new()
            {
                // Latest of this minor version
                ["latest"] = "1",
                ["1"] = ResourceKit.Other.Paths.Compilers.PaintScriptCompiler.p0_1_0
            }
        };

        string compilerVersion = "0.1";

        string SelectedVersionPath = Compilers[compilerVersion][Compilers[compilerVersion]["latest"]];

        // Load compiler.js
        var compilerJs = File.ReadAllText(SelectedVersionPath);
        engine.Execute(compilerJs);

        // compilePaintScript accepts source text, not a file path.
        string scriptSource = File.ReadAllText(scriptPath);
        var result = engine.Invoke(
            "compilePaintScript",
            spriteName,
            instanceName,
            scriptSource,
            StringTools.GetFilenameFromPath(scriptPath)
        );

        return result.AsString();
    }
}
