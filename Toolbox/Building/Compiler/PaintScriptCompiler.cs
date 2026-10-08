using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jint;
using Toolbox.Logging;
using Toolbox.Plumbing;
using Toolbox.Sessions;
using Toolbox.Time;

namespace Toolbox.Building.Compiler.PaintScriptCompiler;

/*
    The PaintScript Compiler

    The PaintScript compiler is written in JavaScript
    and uses Jint to run inside C#. I choose JS because it's easy to write.
    Originally my pick was python, then converting it to wasm then load it in C#.
    But JavaScript is easier, so yeah.

    Anyway, how this works is that JavaScript will compile our PaintScript into
    JSON, then we save the output as a file in the build session directory.
    Afterward, we can link. This will produce a single output json file, our program.
*/

public class PaintScriptCompiler
{
    private static Dictionary<string, Dictionary<string, string>> Compilers = null!;
    public static string Compile(
        string spriteName,
        string instanceName,
        string scriptPath,
        string sessionId,
        BuildSession session)
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

        VerifyOutput(result?.ToString() ?? "!Unknown build failure! Compiler returned null.", instanceName, sessionId, session.BuildPath);

        return "";

        //return result.AsString();
    }

    public static void VerifyOutput(string result, string instance, string sessionKey, string buildPath)
    {
        if (result?[0] == '!')
        {
            throw new Exception(result.Remove(0, 1)); // This is an error
        }
        else
        {
            // Write to disk
           string output = Path.Combine(buildPath, "scripts", $"{instance}{Guid.NewGuid()}.json");
           Log.QuickLog(output);
           File.WriteAllText(output, result);
        }
    }

    public static void Link(string sessionKey, string buildPath)
    {
        string path = Path.Combine(buildPath, "scripts");
        string output = Path.Combine(path, "linked.json");

        if (!Directory.Exists(path))
        {
            Log.QuickLog($"No session directory found for '{sessionKey}'.");
            return;
        }

        string[] scripts = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
            .Where(file => !string.Equals(Path.GetFileName(file), "linked.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (scripts.Length == 0)
        {
            Log.QuickLog($"No compiled sprite files found in session '{sessionKey}'.");
            return;
        }

        JsonNode? linkedVersion = null;
        var globalVariables = new JsonObject();
        var globalFunctions = new JsonObject();
        var targets = new List<JsonObject>();
        var targetsByName = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

        foreach (string script in scripts)
        {
            JsonNode? parsed = JsonNode.Parse(File.ReadAllText(script));
            if (parsed is not JsonObject data)
                throw new InvalidDataException($"Compiled script '{script}' must contain a JSON object.");

            linkedVersion ??= Clone(data["version"]) ?? JsonValue.Create("0.1.0");

            JsonObject? globals = data["globals"] as JsonObject;
            MergeObject(globalVariables, globals?["variables"]);
            MergeObject(globalFunctions, globals?["functions"]);

            string name = data["name"]?.GetValue<string>()
                ?? throw new InvalidDataException($"Compiled script '{script}' is missing its sprite name.");

            if (!targetsByName.TryGetValue(name, out JsonObject? target))
            {
                target = new JsonObject
                {
                    ["name"] = name,
                    ["instance"] = Clone(data["instance"]),
                    ["variables"] = new JsonObject(),
                    ["functions"] = new JsonObject(),
                    ["events"] = new JsonObject()
                };
                targetsByName.Add(name, target);
                targets.Add(target);
            }

            JsonNode? variables = data["vars"] ?? data["variables"];
            MergeObject((JsonObject)target["variables"]!, variables);
            MergeObject((JsonObject)target["functions"]!, data["functions"]);
            MergeEvents((JsonObject)target["events"]!, data["events"], script);
        }

        var linkedProgram = new JsonObject
        {
            ["version"] = linkedVersion,
            ["permissions"] = new JsonObject
            {
                ["filesystem"] = false,
                ["networking"] = false
            },
            ["globals"] = new JsonObject
            {
                ["variables"] = globalVariables,
                ["functions"] = globalFunctions
            },
            ["targets"] = new JsonArray(targets.Select(target => (JsonNode?)target).ToArray())
        };

        Directory.CreateDirectory(path);
        string json = linkedProgram.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(output, IndentByFourSpaces(json));
        Log.QuickLog($"Linked program -> {output}");
    }

    private static void MergeObject(JsonObject target, JsonNode? source)
    {
        if (source is null)
            return;

        if (source is not JsonObject sourceObject)
            throw new InvalidDataException("Expected a JSON object while linking compiled scripts.");

        foreach (KeyValuePair<string, JsonNode?> property in sourceObject)
            target[property.Key] = Clone(property.Value);
    }

    private static void MergeEvents(JsonObject target, JsonNode? source, string script)
    {
        if (source is null)
            return;

        if (source is not JsonObject sourceObject)
            throw new InvalidDataException($"Events in compiled script '{script}' must be a JSON object.");

        foreach (KeyValuePair<string, JsonNode?> eventEntry in sourceObject)
        {
            if (eventEntry.Value is not JsonArray eventList)
                throw new InvalidDataException($"Event '{eventEntry.Key}' in compiled script '{script}' must be an array.");

            if (target[eventEntry.Key] is not JsonArray mergedList)
            {
                mergedList = new JsonArray();
                target[eventEntry.Key] = mergedList;
            }

            foreach (JsonNode? item in eventList)
                mergedList.Add(Clone(item));
        }
    }

    private static JsonNode? Clone(JsonNode? node) =>
        node is null ? null : JsonNode.Parse(node.ToJsonString());

    private static string IndentByFourSpaces(string json)
    {
        string[] lines = json.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            int indentation = 0;
            while (indentation < lines[i].Length && lines[i][indentation] == ' ')
                indentation++;

            if (indentation > 0)
                lines[i] = new string(' ', indentation) + lines[i];
        }

        return string.Join(Environment.NewLine, lines);
    }
}