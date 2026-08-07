// MemoryPrimitives.cs

using System;
using System.Collections.Generic;
using Toolbox.Display.DisplayIntegration;
using PaintPower_VM.VM.Processing.memory;
using PaintPower_VM.VM.Processing.memory.Variables;

namespace PaintPower_VM.VM.Runtime.Primitives.MemoryPrims;

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
        var paramNames = (List<string>)args[1]!;
        string? returnTypeName = args.Count > 2 ? (string?)args[2] : null;
        var body = (List<Instruction>)args[3]!;

        VarType? returnType = returnTypeName != null ? new VarType(returnTypeName) : null;

        var fn = new VmFunction(name, paramNames, returnType, body);
        var slot = new MemorySlot(name, fn);

        // put function in current scope
        thread.ScopeStack.Peek().Variables[name] = slot;

        return null;
    }

    // ---------------------------------
    // mem:method:call
    // ---------------------------------
    private object? MethodCall(string op, List<object?>? args, DIItem item)
    {
        string name = (string)args![0]!;

        // Resolve function from scope/memory
        var slot = thread.ResolveVariable(name);
        if (slot.Item.Value is not VmFunction fn)
            throw new Exception($"'{name}' is not a function.");

        // Evaluate arguments
        var evaluatedArgs = new List<object?>();
        for (int i = 0; i < fn.ParameterNames.Count; i++)
        {
            object? rawArg = args![i + 1];
            object? val = PrimitiveHelpers.Eval(rawArg, thread, item);
            evaluatedArgs.Add(val);
        }

        // Enter function scope
        thread.EnterScope();
        var funcScope = thread.ScopeStack.Peek();

        // Bind parameters as variables
        for (int i = 0; i < fn.ParameterNames.Count; i++)
        {
            string paramName = fn.ParameterNames[i];
            var varType = new VarType("auto"); // or param type if you add it
            var paramVar = new Variable(varType, true, evaluatedArgs[i]);
            funcScope.Variables[paramName] = new MemorySlot(paramName, paramVar);
        }

        // Execute body
        thread.IsReturningFromFunction = false;
        thread.FunctionReturnValue = null;

        foreach (var instr in fn.Body)
        {
            var prim = thread._interpreter!.functions[instr.OpCode];
            prim(instr.OpCode, instr.args, item);

            if (thread.IsReturningFromFunction)
                break;
        }

        object? returnValue = thread.FunctionReturnValue;

        // Enforce return type if needed
        if (fn.ReturnType != null && returnValue == null)
            throw new Exception($"Function '{fn.Name}' must return '{fn.ReturnType.Name}'.");

        // Exit function scope
        thread.ExitScope();

        return returnValue;
    }

}
