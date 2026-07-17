// Variable.cs

using System;
using System.Collections.Generic;
using PaintPower.Vm.Runtime;

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

public class VmFunction : MemoryItem
{
    public string Name { get; }
    public List<string> ParameterNames { get; }
    public VarType? ReturnType { get; }
    public List<Instruction> Body { get; }

    public VmFunction(
        string name,
        List<string> parameterNames,
        VarType? returnType,
        List<Instruction> body
    )
    {
        Name = name;
        ParameterNames = parameterNames;
        ReturnType = returnType;
        Body = body;

        // MemoryItem fields:
        Type = returnType;      // or a special "func" type
        IsMutable = false;      // usually functions are immutable
        Value = this;           // the value *is* the function object
    }
}
