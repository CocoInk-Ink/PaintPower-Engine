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

    private static bool SafeToBool(object? value)
    {
        if (value == null)
            return false;

        try
        {
            // Numbers: 0 = false, nonzero = true
            if (value is int i)
                return i != 0;
            if (value is double d)
                return d != 0.0;

            // Strings: empty or "0" = false
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return false;
                if (s == "0")
                    return false;
                if (bool.TryParse(s, out bool b))
                    return b;
                return true; // any non-empty, non-"0" string is true
            }

            // Fallback to Convert
            return Convert.ToBoolean(value);
        }
        catch
        {
            return false;
        }
    }

    private static double SafeToDouble(object? value)
    {
        if (value == null)
            return 0;

        try
        {
            if (value is int i)
                return i;
            if (value is double d)
                return d;
            if (value is string s && double.TryParse(s, out double parsed))
                return parsed;

            return Convert.ToDouble(value);
        }
        catch
        {
            return 0;
        }
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

            case OpCode.JumpIfFalse:
                {
                    var value = _stack.Pop();
                    bool cond = SafeToBool(value);
                    if (!cond)
                        _ip = instr.Operand;
                    else
                        _ip++;
                    return;
                }

            case OpCode.Jump:
                _ip = instr.Operand;
                return;

            case OpCode.CompareEqual:
                {
                    var b = _stack.Pop();
                    var a = _stack.Pop();
                    _stack.Push(Equals(a, b));
                    break;
                }

            case OpCode.CompareNotEqual:
                {
                    var b = _stack.Pop();
                    var a = _stack.Pop();
                    _stack.Push(!Equals(a, b));
                    break;
                }

            case OpCode.CompareLess:
                {
                    var b = SafeToDouble(_stack.Pop());
                    var a = SafeToDouble(_stack.Pop());
                    _stack.Push(a < b);
                    break;
                }

            case OpCode.CompareGreater:
                {
                    var b = SafeToDouble(_stack.Pop());
                    var a = SafeToDouble(_stack.Pop());
                    _stack.Push(a > b);
                    break;
                }

            case OpCode.CompareLessEqual:
                {
                    var b = SafeToDouble(_stack.Pop());
                    var a = SafeToDouble(_stack.Pop());
                    _stack.Push(a <= b);
                    break;
                }

            case OpCode.CompareGreaterEqual:
                {
                    var b = SafeToDouble(_stack.Pop());
                    var a = SafeToDouble(_stack.Pop());
                    _stack.Push(a >= b);
                    break;
                }

        }

        _ip++;
    }
}
