// File: PaintPower.Vm.Runtime.Interpreter/Interpreter.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintPower.Vm;
using PaintPower.Vm.Runtime.Sprites;
using PaintPower.Display.DisplayIntegration;

namespace PaintPower.Vm.Runtime.Interpreter;

public sealed class Interpreter
{
    private readonly List<Instruction> _code;
    private int _ip;
    private readonly VmThread _thread; // Parent.

    // It's "diplay", not a typo, don't you dare change it. "DisplayIntegration Item" (DII),
    // An item for the "DisplayIntegration Player" (DIPlay/Diplay)
    private readonly DIItem diplay_item;

    public bool IsFinished { get; private set; }
    public Primitives.Primitives primitives = new();
    public Dictionary<string, Func<string, List<object?>?, DIItem, object?>> functions = new();

    public Interpreter(List<Instruction> code, VmThread thread, DIItem diplay_item)
    {
        _code = code ?? throw new ArgumentNullException(nameof(code));
        _thread = thread ?? throw new ArgumentNullException(nameof(thread));
        this.diplay_item = diplay_item ?? throw new ArgumentNullException(nameof(diplay_item));
        _ip = 0;
        IsFinished = false;

        this.diplay_item = diplay_item;

        addPrimsTo(functions);
    }

    public void addPrimsTo(Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable)
    {
        new Primitives.Primitives().addPrimsTo(primTable);
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
}
