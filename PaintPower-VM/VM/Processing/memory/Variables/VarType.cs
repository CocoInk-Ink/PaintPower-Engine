// VarType.cs
using System;

namespace PaintPower_VM.VM.Processing.memory.Variables;

public class VarType
{
    public static string[] GeneralBuiltins =
        { "int", "float", "double", "short", "long", "byte", "char", "array", "object" };

    public string Name { get; }

    public VarType(string name)
    {
        Name = name;
    }

    public bool IsBuiltin => Array.IndexOf(GeneralBuiltins, Name) >= 0;
}
