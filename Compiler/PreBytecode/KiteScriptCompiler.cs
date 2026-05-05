using System;
using PaintPower.Runtime.Bytecode;
using PaintPower.Vm.Processing;

namespace PaintPower.Compiler.PreBytecode;

public static class KiteScriptCompiler
{
    public static Bytecode Compile(string text)
    {
        var nodes = KiteScriptParser.Parse(text);
        var set = new InstructionSet();

        foreach (var node in nodes)
        {
            switch (node)
            {
                case KsCall call:
                    if (!BuiltinRegistry.TryEmit(call.TargetId, set, call.Args))
                        throw new Exception($"Unknown builtin: {call.TargetId}");
                    break;

                case KsInstruction inst:
                    // You can expand this later
                    break;
            }
        }

        Console.WriteLine("Parsed nodes: " + nodes.Count);
        foreach (var n in nodes)
        {
            Console.WriteLine("Node: " + n.GetType().Name);
        }

        set.Emit(OpCode.Return);
        return set.ToBytecode();
    }
}
