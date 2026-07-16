using System;
using System.Collections.Generic;
using PaintPower.Display.DisplayIntegration;
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
}

public class Primitives
{
    public void addPrimsTo(Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable)
    {
        // Operators
        PrimitiveHelpers.StickTogether(primTable, Arithmetic, "math:add", "math:sub", "math:mul", "math:div", "math:mod");

        // Comparison
        PrimitiveHelpers.StickTogether(primTable, Compare, ">", "<", "=>", ">=", "<=", "=<", "==", "===");

        new MemoryPrims.MemoryPrimitives().addPrimsTo(primTable);
        new JavaScriptPrims.JSPrimitives().addPrimsTo(primTable);
    }

    private static object? Arithmetic(string op, List<object?>? args, DIItem item)
    {
        object? a, b;

        if (args == null) throw new Exception("Params are required for this function");

        a = args[0];
        b = args[1];

        if (a == null || b == null) throw new Exception("Two parameters are required");

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

        if (a is double || b is double)
        {
            double ad = Convert.ToDouble(a ?? 0);
            double bd = Convert.ToDouble(b ?? 0);
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

        // string concat for Add
        if (op == "math:add" && a is string astr && b is string bstr)
            return $"{astr}{bstr}";

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
}