// Block.cs

using System;
using System.Collections.Generic;
using PaintPower_VM.VM.Processing.memory;
using PaintPower_VM.VM.Processing.memory.Variables;

namespace PaintPower_VM.VM.Processing;

public class ScopeBlock
{
    public Dictionary<string, MemorySlot> Variables { get; } = new();
    public ScopeBlock? Parent { get; set; }

    public bool TryGetVariable(string name, out MemorySlot slot)
    {
        if (Variables.TryGetValue(name, out slot))
            return true;

        if (Parent != null)
            return Parent.TryGetVariable(name, out slot);

        slot = null!;
        return false;
    }
}
