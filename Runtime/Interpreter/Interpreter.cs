using System;
using System.Collections.Generic;
using PaintPower.Logging;
using PaintPower.Runtime.Bytecode;

namespace PaintPower.Runtime.Interpreter;

public class Interpreter
{
    private readonly Bytecode.Bytecode _code;
    private readonly Stack<object> _stack = new();
    private readonly object[] _locals;
    private int _ip = 0; // instruction pointer

    public bool IsFinished => _ip >= _code.Instructions.Length;

    public Interpreter(Bytecode.Bytecode code)
    {
        _code = code;
        _locals = new object[_code.LocalCount];
    }

    public void Step()
    {
        if (IsFinished)
            return;

        var instr = _code.Instructions[_ip];

        switch (instr.OpCode)
        {
            case OpCode.Nop:
                break;

            case OpCode.LoadConst:
                _stack.Push(_code.Constants[instr.Operand]);
                break;

            case OpCode.LoadLocal:
                _stack.Push(_locals[instr.Operand]);
                break;

            case OpCode.StoreLocal:
                _locals[instr.Operand] = _stack.Pop();
                break;

            case OpCode.Return:
                _ip = _code.Instructions.Length;
                return;

            case OpCode.Print:
                {
                    var value = _stack.Pop();
                    Log.QuickLog(value);
                    break;
                }

        }

        _ip++;
    }
}
