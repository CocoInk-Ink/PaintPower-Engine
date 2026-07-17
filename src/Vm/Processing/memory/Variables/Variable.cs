// Variable.cs

using System;

namespace PaintPower.Vm.Processing.memory.Variables;

public class Variable : MemoryItem
{

    public bool IsReadonly = false;
    public bool HasBeenAssigned = false;

    public Variable(VarType? type = null, bool isMutable = true, object? initialValue = null)
    {
        Type = type;
        IsMutable = isMutable;
        Value = initialValue;
    }
}
