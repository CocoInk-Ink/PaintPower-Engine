using System;
using System.Collections.Generic;
using PaintPower.Runtime.Bytecode;
using PaintPower.Vm.Processing;

namespace PaintPower.Compiler.PreBytecode;

public static class BuiltinRegistry
{
    private static readonly Dictionary<string, Action<InstructionSet, List<string>>> _builtins =
        new()
        {
            ["PopularIds.functions.print"] = EmitPrint
        };

    public static bool TryEmit(string id, InstructionSet set, List<string> args)
    {
        if (_builtins.TryGetValue(id, out var emitter))
        {
            emitter(set, args);
            return true;
        }
        return false;
    }

    private static void EmitPrint(InstructionSet set, List<string> args)
    {
        if (args.Count > 0)
        {
            int c = set.AddConstant(args[0]);
            set.Emit(OpCode.LoadConst, c);
        }

        set.Emit(OpCode.Print);
    }
}
