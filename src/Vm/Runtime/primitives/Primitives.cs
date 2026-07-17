using System;
using System.Collections.Generic;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Logging;
using PaintPower.Vm.Runtime.Sprites;

namespace PaintPower.Vm.Runtime.Primitives;

public static class PrimitiveHelpers
{
    // When multiple opcodes use the same function, it's not really preferred to write
    // the same code over and over. This function reduces redundancy.
    public static void StickTogether(
        Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable,
        Func<string, List<object?>?, DIItem, object?> func,
        params string[] opcodes
    )
    {
        foreach (var opcode in opcodes)
        {
            primTable[opcode] = func;
        }
    }

    public static object? Eval(object? expr, VmThread thread, DIItem item)
    {
        if (thread == null || thread?._interpreter == null)
        {
            Log.QuickLog($"Interpreter or thread is null. Thread: {thread}, Interpreter: {thread?._interpreter}");
            return null;
        }

        Log.QuickLog($"expr: {expr}");

        // Literal value → return as-is
        if (expr == null) return null;

        // If it's not an Instruction, it's a literal
        if (expr is not Instruction instr)
            return expr;

        // Evaluate nested instruction
        var fn = thread._interpreter.functions[instr.OpCode];
        return fn(instr.OpCode, instr.args, item);
    }

}

public class Primitives
{
    private readonly VmThread thread;

    public Primitives(VmThread vmThread)
    {
        thread = vmThread;
    }

    public void addPrimsTo(Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable)
    {
        // Arithmetic
        PrimitiveHelpers.StickTogether(primTable, Arithmetic,
            "math:add", "math:sub", "math:mul", "math:div", "math:mod");

        // Comparison
        PrimitiveHelpers.StickTogether(primTable, Compare,
            ">", "<", "=>", ">=", "<=", "=<", "==", "===");

        // String ops
        PrimitiveHelpers.StickTogether(primTable, StringOps,
            "str:len", "str:upper", "str:lower", "str:trim", "str:concat");

        // Boolean ops
        PrimitiveHelpers.StickTogether(primTable, BoolOps,
            "bool:not", "bool:and", "bool:or");

        // Type conversion
        PrimitiveHelpers.StickTogether(primTable, ConvertOps,
            "to:int", "to:float", "to:double", "to:string", "to:bool");

        // Null / undefined
        PrimitiveHelpers.StickTogether(primTable, NullOps,
            "null", "undefined");

        // Identity / nop
        primTable["id"] = Identity;
        primTable["nop"] = Nop;

        // Generic DIItem ops
        primTable["di:setPos"] = DISetPos;
        primTable["di:setVisible"] = DISetVisible;

        // Language-specific prims added later
        new MemoryPrims.MemoryPrimitives(thread).addPrimsTo(primTable);
        new JavaScriptPrims.JSPrimitives(thread).addPrimsTo(primTable);
    }

    private object? Arithmetic(string op, List<object?>? args, DIItem item)
    {
        if (args == null || args.Count < 2)
            throw new Exception("Two parameters are required for arithmetic");

        // Evaluate nested expressions
        object? a = PrimitiveHelpers.Eval(args[0], thread, item);
        object? b = PrimitiveHelpers.Eval(args[1], thread, item);

        // Treat null as 0 (important!)
        a ??= 0;
        b ??= 0;

        // int math
        if (a is int ai && b is int bi)
        {
            return op switch
            {
                "math:add" => ai + bi,
                "math:sub" => ai - bi,
                "math:mul" => ai * bi,
                "math:div" => bi == 0 ? 0 : ai / bi,
                "math:mod" => bi == 0 ? 0 : ai % bi,
                _ => 0
            };
        }

        // double math
        if (a is double || b is double)
        {
            double ad = Convert.ToDouble(a);
            double bd = Convert.ToDouble(b);

            return op switch
            {
                "math:add" => ad + bd,
                "math:sub" => ad - bd,
                "math:mul" => ad * bd,
                "math:div" => bd == 0 ? 0.0 : ad / bd,
                "math:mod" => bd == 0 ? 0.0 : ad % bd,
                _ => 0.0
            };
        }

        // string concat
        if (op == "math:add" && a is string astr && b is string bstr)
            return astr + bstr;

        return 0;
    }

    private static object? Compare(string op, List<object?>? args, DIItem item)
    {

        object? left, right;

        if (args == null) throw new Exception("Params are required for this function");

        left = args[0];
        right = args[1];

        if (left == null || right == null) throw new Exception("Two parameters are required");


        int cmp = Comparer<object?>.Default.Compare(left, right);
        return op switch
        {
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            "<" => cmp < 0,
            "<=" => cmp <= 0,
            "=<" => cmp <= 0,
            ">" => cmp > 0,
            ">=" => cmp >= 0,
            "=>" => cmp >= 0,
            _ => false
        };
    }

    private static object? StringOps(string op, List<object?>? args, DIItem item)
    {
        if (args == null || args.Count == 0)
            throw new Exception("Params are required");

        return op switch
        {
            "str:len" => args[0]?.ToString()?.Length ?? 0,
            "str:upper" => args[0]?.ToString()?.ToUpper(),
            "str:lower" => args[0]?.ToString()?.ToLower(),
            "str:trim" => args[0]?.ToString()?.Trim(),
            "str:concat" => $"{args[0]}{args[1]}",
            _ => null
        };
    }

    private static object? BoolOps(string op, List<object?>? args, DIItem item)
    {
        bool a = Convert.ToBoolean(args![0]);
        bool b = args.Count > 1 ? Convert.ToBoolean(args[1]) : false;

        return op switch
        {
            "bool:not" => !a,
            "bool:and" => a && b,
            "bool:or" => a || b,
            _ => false
        };
    }

    private static object? ConvertOps(string op, List<object?>? args, DIItem item)
    {
        object? v = args![0];

        return op switch
        {
            "to:int" => Convert.ToInt32(v),
            "to:float" => Convert.ToSingle(v),
            "to:double" => Convert.ToDouble(v),
            "to:string" => v?.ToString(),
            "to:bool" => Convert.ToBoolean(v),
            _ => null
        };
    }

    private static object? NullOps(string op, List<object?>? args, DIItem item)
    {
        return op switch
        {
            "null" => null,
            "undefined" => null, // JS-style undefined
            _ => null
        };
    }

    private static object? Identity(string op, List<object?>? args, DIItem item)
    {
        return args != null && args.Count > 0 ? args[0] : null;
    }

    private static object? Nop(string op, List<object?>? args, DIItem item)
    {
        return null;
    }

    private object? DISetPos(string op, List<object?>? args, DIItem item)
    {
        item.x = Convert.ToDouble(args![0]);
        item.y = Convert.ToDouble(args[1]);
        return null;
    }

    private object? DISetVisible(string op, List<object?>? args, DIItem item)
    {
        item.IsVisible = Convert.ToBoolean(args![0]);
        return null;
    }

}