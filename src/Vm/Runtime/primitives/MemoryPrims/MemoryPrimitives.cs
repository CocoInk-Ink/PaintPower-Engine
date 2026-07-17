// MemoryPrimitives.cs
using System;
using System.Collections.Generic;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Vm.Processing.memory;
using PaintPower.Vm.Processing.memory.Variables;

namespace PaintPower.Vm.Runtime.Primitives.MemoryPrims;

public class MemoryPrimitives
{
    private readonly VmThread thread;

    public MemoryPrimitives(VmThread vmThread)
    {
        thread = vmThread;
    }

    public void addPrimsTo(Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable)
    {
        primTable["mem:getVar"]    = MemGetVar;
        primTable["mem:setVar"]    = MemSetVar;
        primTable["mem:declareVar"] = MemDeclareVar;
        primTable["mem:exists"]    = MemExists;
        primTable["mem:delete"]    = MemDelete;
        primTable["mem:pushSlot"]  = MemPushSlot;
        primTable["mem:pullSlot"]  = MemPullSlot;
    }

    // -----------------------------
    // mem:getVar
    // -----------------------------
    private object? MemGetVar(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        var slot = thread.ResolveVariable(name);
        return slot.Item.Value;
    }

    // -----------------------------
    // mem:setVar
    // -----------------------------
    private object? MemSetVar(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        object? value = args[1];

        thread.memory.PushValue(name, value);
        return null;
    }

    // -----------------------------
    // mem:declareVar
    // Generic variable declaration
    // -----------------------------
    private object? MemDeclareVar(string op, List<object?>? args, DIItem item)
    {
        // args: [name, typeName, mutableFlag]
        string name = (string)args![0]!;
        string typeName = (string)args[1]!;
        bool isMutable = args.Count > 2 ? Convert.ToBoolean(args[2]) : true;

        var type = new VarType(typeName);

        thread.DeclareVariable(name, type, isMutable, false);
        return null;
    }

    // -----------------------------
    // mem:exists
    // -----------------------------
    private object? MemExists(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        try
        {
            thread.ResolveVariable(name);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // -----------------------------
    // mem:delete
    // -----------------------------
    private object? MemDelete(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        thread.memory.FreeSlot(name);
        return null;
    }

    // -----------------------------
    // mem:pushSlot
    // Create a raw memory slot manually
    // -----------------------------
    private object? MemPushSlot(string op, List<object?>? args, DIItem item)
    {
        string slotId = (string)args![0]!;
        object? value = args.Count > 1 ? args[1] : null;

        thread.memory.PushValue(slotId, value);
        return null;
    }

    // -----------------------------
    // mem:pullSlot
    // Return the raw MemorySlot object
    // -----------------------------
    private object? MemPullSlot(string op, List<object?>? args, DIItem item)
    {
        string slotId = (string)args![0]!;
        if (thread.memory.MemorySlots.TryGetValue(slotId, out var slot))
            return slot;

        return null;
    }
}
