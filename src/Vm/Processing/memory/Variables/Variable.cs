// Variable.cs

using System;

namespace PaintPower.Vm.Processing.memory.Variables;

public class Variable : MemoryItem
{
    public bool IsMutable { get; set; } = true;
    public VarType? Type { get; set; }

    public Variable(VarType? type = null, bool isMutable = true, object? initialValue = null)
    {
        Type = type;
        IsMutable = isMutable;
        Value = initialValue;
    }
}
