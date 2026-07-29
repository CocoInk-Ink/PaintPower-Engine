// VmThread.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Vm.Runtime.Interpreter;
using PaintPower.Vm.Processing;
using PaintPower.Vm.Processing.memory;
using PaintPower.Vm.Runtime;
using PaintPower.Vm.Processing.memory.Variables;
using System.Linq;

namespace PaintPower.Vm;

public class VmThread
{

    public bool isPaused = false;
    public Interpreter? _interpreter;

    public bool IsWaiting = false;
    public DateTime? WakeAt = null;
    public bool IsYielded = false;
    public bool IsFinished = false;
    public string Id { get; set; } = Guid.NewGuid().ToString();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    // Code effects who?
    public DIItem target;

    public Memory memory; // Passed by a VM, don't init.
    public Stack<ScopeBlock> ScopeStack { get; } = new(); // How deep are we inside?

    public HashSet<string> ActiveQuirks { get; } = new();

    public Vm parent;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

public object? FunctionReturnValue { get; set; }
public bool IsReturningFromFunction { get; set; }

public VmFuture? AwaitingFuture { get; set; }
public VmFuture? Future { get; set; }


    public void Load(Vm vm, List<Instruction> code, DIItem target)
    {

        if (ScopeStack.Count == 0)
            EnterScope();

        _interpreter = new Interpreter(code, this, target);

        parent = vm;

        if (target == null) return;
        this.target = target;
    }

    public void EnterScope()
    {
        var newScope = new ScopeBlock();
        if (ScopeStack.Count > 0)
            newScope.Parent = ScopeStack.Peek();

        ScopeStack.Push(newScope);
    }

    public void ExitScope()
    {
        ScopeStack.Pop();
    }

    public void DeclareVariable(string name, VarType type, bool isMutable, bool isHoisted)
    {
        ScopeBlock targetScope;

        if (ActiveQuirks.Contains("js:hoist") && isHoisted)
        {
            // hoist to function scope
            targetScope = ScopeStack.First(); // bottom of stack
        }
        else
        {
            // normal block scope
            targetScope = ScopeStack.Peek();
        }

        var slot = new MemorySlot(name, new MemoryItem
        {
            Type = type,
            IsMutable = isMutable,
            Value = null
        });

        targetScope.Variables[name] = slot;
    }

    public MemorySlot ResolveVariable(string name)
    {
        foreach (var scope in ScopeStack)
        {
            if (scope.Variables.TryGetValue(name, out var slot))
                return slot;
        }

        // JS hoisted var: use before declaration
        if (ActiveQuirks.Contains("js:hoist"))
        {
            // create in function scope with undefined value
            var functionScope = ScopeStack.First();
            var slot = new MemorySlot(name, new MemoryItem
            {
                Type = null,
                IsMutable = true,
                Value = null // JS 'undefined'
            });
            functionScope.Variables[name] = slot;
            return slot;
        }

        // sloppy JS mode: undeclared becomes global
        if (ActiveQuirks.Contains("js:sloppy"))
        {
            memory.PushValue(name, null);
            return memory.MemorySlots[name];
        }

        throw new Exception($"Variable '{name}' not found.");
    }

    public async Task Step()
    {
        if (_interpreter == null || isPaused || IsWaiting || IsFinished)
            return;

        _interpreter.Step();

        // reset yield flag so next tick can run again
        IsYielded = false;

        if (_interpreter.IsFinished)
            isPaused = true;
    }
}
