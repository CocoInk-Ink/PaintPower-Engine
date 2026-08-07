// File: PaintPower_VM.VM.Runtime.Interpreter/Interpreter.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintPower_VM.VM;
using Toolbox.Display.Sprites;
using Toolbox.Display.DisplayIntegration;
using PaintPower_VM.VM.Runtime.Primitives;

namespace PaintPower_VM.VM.Runtime.Interpreter;

public sealed class Interpreter
{
    private readonly List<Instruction> _code;
    private int _ip;
    private readonly VmThread _thread; // Parent.

    // It's "diplay", not a typo, don't you dare change it. "DisplayIntegration Item" (DII),
    // An item for the "DisplayIntegration Player" (DIPlay/Diplay)
    private readonly DIItem diplay_item;

    public bool IsFinished { get; private set; }
    public Primitives.Primitives primitives;
    public Dictionary<string, Func<string, List<object?>?, DIItem, object?>> functions = new();

    public int CurrentIp => _ip;
    public void JumpTo(int ip) => _ip = ip;

    public Interpreter(List<Instruction> code, VmThread thread, DIItem diplay_item)
    {
        _code = code ?? throw new ArgumentNullException(nameof(code));
        _thread = thread ?? throw new ArgumentNullException(nameof(thread));
        this.diplay_item = diplay_item ?? throw new ArgumentNullException(nameof(diplay_item));
        _ip = 0;
        IsFinished = false;

        this.diplay_item = diplay_item;

        // Primitives
        primitives = new(thread);
        addPrimsTo(functions);
    }

    public void addPrimsTo(Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable)
    {

        primTable["jump"] = PrimJump;
        primTable["jumpIfFalse"] = PrimJumpIfFalse;
        primTable["call"] = PrimCall;
        primTable["ret"] = PrimRet;
        primTable["yield"] = PrimYield;
        primTable["halt"] = PrimHalt;

        primitives.addPrimsTo(primTable);
    }

    public void Step()
    {
        if (IsFinished) return;
        if (_ip < 0 || _ip >= _code.Count)
        {
            Finish();
            return;
        }

        var instr = _code[_ip++];
        var function = functions[instr.OpCode];

        function(instr.OpCode, instr.args, diplay_item);

    }

    private void Finish()
    {
        IsFinished = true;
        _thread.IsFinished = true;
    }

    private object? PrimJump(string op, List<object?>? args, DIItem item)
    {
        int target = Convert.ToInt32(args![0]);
        _ip = target;
        return null;
    }

    private object? PrimJumpIfFalse(string op, List<object?>? args, DIItem item)
    {
        bool cond = Convert.ToBoolean(args![0]);
        int target = Convert.ToInt32(args[1]);

        if (!cond)
            _ip = target;

        return null;
    }

    private object? PrimCall(string op, List<object?>? args, DIItem item)
    {
        int target = Convert.ToInt32(args![0]);

        // later: push return address on call stack
        _ip = target;
        return null;
    }

    private object? PrimRet(string op, List<object?>? args, DIItem item)
    {
        object? value = null;
        if (args != null && args.Count > 0)
            value = PrimitiveHelpers.Eval(args[0], _thread, item);

        // Complete future if async
        if (_thread.Future != null)
        {
            _thread.Future.IsCompleted = true;
            _thread.Future.Result = value;
        }

        // Store return value for the function executor
        _thread.FunctionReturnValue = value;
        _thread.IsReturningFromFunction = true;

        _thread.IsFinished = true;
        IsFinished = true;

        return value;
    }


    private object? PrimYield(string op, List<object?>? args, DIItem item)
    {
        _thread.IsYielded = true;
        return null;
    }

    private object? PrimHalt(string op, List<object?>? args, DIItem item)
    {
        _thread.IsFinished = true;
        IsFinished = true;
        return null;
    }
}
