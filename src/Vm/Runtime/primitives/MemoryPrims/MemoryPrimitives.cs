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
        primTable["mem:getVar"] = MemGetVar;
        primTable["mem:setVar"] = MemSetVar;
        primTable["mem:declareVar"] = MemDeclareVar;
        primTable["mem:exists"] = MemExists;
        primTable["mem:delete"] = MemDelete;
        primTable["mem:pushSlot"] = MemPushSlot;
        primTable["mem:pullSlot"] = MemPullSlot;
        primTable["mem:method:declare"] = MethodDeclare;
        primTable["mem:method:call"] = MethodCall;
    }

    // -----------------------------
    // mem:getVar
    // -----------------------------
    private object? MemGetVar(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        var slot = thread.ResolveVariable(name);
        return slot.Item.Value ?? thread.memory.GetValue(name);
    }

    // -----------------------------
    // mem:setVar
    // -----------------------------
    private object? MemSetVar(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        object? rawValue = args[1];

        object? value = PrimitiveHelpers.Eval(rawValue, thread, item);

        try
        {
            var slot = thread.ResolveVariable(name);
            slot.Item.Value = value;
        }
        catch
        {
            // fall back to the shared memory map if the variable has not been declared yet
        }

        thread.memory.PushValue(name, value);

        return null;
    }

    // -----------------------------
    // mem:declareVar
    // -----------------------------
    private object? MemDeclareVar(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        string typeName = (string)args[1]!;
        bool isMutable = args.Count > 2 ? Convert.ToBoolean(args[2]) : true;

        var type = new VarType(typeName);

        thread.DeclareVariable(name, type, isMutable, false);

        // Optional initial value
        if (args.Count > 3)
        {
            object? initial = PrimitiveHelpers.Eval(args[3], thread, item);
            var slot = thread.ResolveVariable(name);
            slot.Item.Value = initial;
            thread.memory.PushValue(name, initial);
        }

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
    // -----------------------------
    private object? MemPushSlot(string op, List<object?>? args, DIItem item)
    {
        string slotId = (string)args![0]!;
        object? rawValue = args.Count > 1 ? args[1] : null;

        object? value = PrimitiveHelpers.Eval(rawValue, thread, item);

        thread.memory.PushValue(slotId, value);
        return null;
    }

    // -----------------------------
    // mem:pullSlot
    // -----------------------------
    private object? MemPullSlot(string op, List<object?>? args, DIItem item)
    {
        string slotId = (string)args![0]!;
        if (thread.memory.MemorySlots.TryGetValue(slotId, out var slot))
            return slot;

        return null;
    }

    // ------------------------------
    // mem:method:declare
    // ------------------------------

    private object? MethodDeclare(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;
        int startIp = Convert.ToInt32(args[1]);

        thread.Methods[name] = startIp;
        return null;
    }

    // ---------------------------------
    // mem:method:call
    // ---------------------------------
    private object? MethodCall(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;

        // Look up method start IP
        if (!thread.Methods.TryGetValue(name, out int targetIp))
            throw new Exception($"Method '{name}' not found.");

        // Save return address
        thread.CallStack.Push(thread._interpreter!.CurrentIp);

        // Optional: store parameters as arg1, arg2, arg3...
        for (int i = 1; i < args.Count; i++)
        {
            object? value = PrimitiveHelpers.Eval(args[i], thread, item);
            thread.memory.PushValue($"arg{i}", value);
        }

        // Jump to method start
        thread._interpreter.JumpTo(targetIp);

        return null;
    }

}
