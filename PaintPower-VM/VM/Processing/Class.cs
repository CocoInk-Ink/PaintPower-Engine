// Class.cs

using System;
using System.Collections.Generic;
using PaintPower_VM.VM.Processing.memory.Variables;

namespace PaintPower_VM.VM.Processing.memory;

public class VmObject
{
    public ClassDescriptor Class { get; }
    public Dictionary<string, MemoryItem> Fields { get; } = new();

    public VmObject(ClassDescriptor cls)
    {
        Class = cls;

        // allocate fields
        foreach (var field in cls.Fields)
        {
            Fields[field.Key] = new MemoryItem
            {
                Type = field.Value,
                Value = null,
                IsMutable = true
            };
        }
    }
}

public class ClassDescriptor
{
    public string Name { get; set; } = "";
    public Dictionary<string, VarType> Fields { get; } = new();
    public Dictionary<string, string> Methods { get; } = new();
    // Methods map to prim names or function IDs
}