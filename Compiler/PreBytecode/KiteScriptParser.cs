// KiteScriptParser.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PaintPower.Compiler.PreBytecode;

public static class KiteScriptParser
{
    private static readonly Regex InstructionRegex =
        new(@"\[#(?<name>\w+)\s+(?<params>.*?)\]", RegexOptions.Compiled);

    private static readonly Regex CallRegex =
        new(@"\[#call\s+(?<id>[^\]]+)\]\((?<args>.*?)\);", RegexOptions.Compiled);

    public static List<KsNode> Parse(string text)
    {
        var nodes = new List<KsNode>();
        var lines = text.Split('\n');

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("@@!"))
                continue;

            // CALL
            var callMatch = CallRegex.Match(line);
            if (callMatch.Success)
            {
                var id = callMatch.Groups["id"].Value.Trim('"');
                var args = Regex.Matches(callMatch.Groups["args"].Value, "\"(.*?)\"")
    .Select(m => m.Groups[1].Value)
    .ToList();

                var call = new KsCall
                {
                    TargetId = id
                };
                call.Args.AddRange(args);
                nodes.Add(call);

                continue;
            }

            // INSTRUCTION
            var instMatch = InstructionRegex.Match(line);
            if (instMatch.Success)
            {
                var name = instMatch.Groups["name"].Value;
                var paramString = instMatch.Groups["params"].Value;

                var parameters = Regex.Matches(paramString, "\"(.*?)\"")
                    .Select(m => m.Groups[1].Value)
                    .ToList();

                var inst = new KsInstruction
                {
                    Name = name
                };
                inst.Params.AddRange(parameters);
                nodes.Add(inst);

                continue;
            }
        }

        return nodes;
    }
}
