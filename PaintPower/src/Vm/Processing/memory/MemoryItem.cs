// MemoryItem.cs

using System;
using PaintPower.Vm.Processing.memory.Variables;

namespace PaintPower.Vm.Processing.memory;

public class MemoryItem
{
    public object? Value { get; set; }
    public VarType? Type { get; set; }
    public bool IsMutable { get; set; } = true;

    public bool IsObject => Value is VmObject;
    public bool IsPrimitive => !(Value is VmObject);
}
