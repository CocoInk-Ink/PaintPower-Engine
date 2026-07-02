// File: PaintPower.Runtime.Interpreter/Interpreter.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintPower.Runtime.Ksa;
using PaintPower.Vm;
using PaintPower.Sprites;

namespace PaintPower.Runtime.Interpreter
{
    public sealed class Interpreter
    {
        private readonly Bytecode _code;
        private int _ip;
        private readonly Stack<object?> _eval = new();
        private readonly List<CallFrame> _callStack = new();
        private readonly object?[] _locals;
        private readonly VmThread _thread;
        private readonly RuntimeBridge _runtime;

        public bool IsFinished { get; private set; }

        public Interpreter(Bytecode code, VmThread thread, RuntimeBridge runtime)
        {
            _code = code ?? throw new ArgumentNullException(nameof(code));
            _thread = thread ?? throw new ArgumentNullException(nameof(thread));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _locals = new object?[code.LocalCount];
            _ip = 0;
            IsFinished = false;
        }

        public void Step()
        {
            if (IsFinished) return;
            if (_ip < 0 || _ip >= _code.Instructions.Length)
            {
                Finish();
                return;
            }

            var instr = _code.Instructions[_ip++];
            switch (instr.OpCode)
            {
                case OpCode.Nop:
                    break;

                case OpCode.PushConst:
                case OpCode.LoadConstIndex:
                    {
                        var c = _code.Constants[instr.Operand];
                        _eval.Push(c);
                        break;
                    }

                case OpCode.PushNull:
                    _eval.Push(null);
                    break;

                case OpCode.LoadLocal:
                    {
                        int idx = instr.Operand;
                        _eval.Push(_locals[idx]);
                        break;
                    }

                case OpCode.StoreLocal:
                    {
                        int idx = instr.Operand;
                        var v = _eval.Pop();
                        _locals[idx] = v;
                        break;
                    }

                case OpCode.Add:
                case OpCode.Sub:
                case OpCode.Mul:
                case OpCode.Div:
                    {
                        var b = _eval.Pop();
                        var a = _eval.Pop();
                        _eval.Push(Arithmetic(instr.OpCode, a, b));
                        break;
                    }

                case OpCode.CompareEqual:
                case OpCode.CompareNotEqual:
                case OpCode.CompareLess:
                case OpCode.CompareLessEqual:
                case OpCode.CompareGreater:
                case OpCode.CompareGreaterEqual:
                    {
                        var right = _eval.Pop();
                        var left = _eval.Pop();
                        _eval.Push(Compare(instr.OpCode, left, right));
                        break;
                    }

                case OpCode.Jump:
                    _ip = instr.Operand;
                    break;

                case OpCode.JumpIfFalse:
                    {
                        var cond = _eval.Pop();
                        if (!IsTruthy(cond)) _ip = instr.Operand;
                        break;
                    }

                case OpCode.Call:
                    {
                        // operand is target IP
                        _callStack.Add(new CallFrame(_ip));
                        _ip = instr.Operand;
                        break;
                    }

                case OpCode.Ret:
                    {
                        object? ret = _eval.Count > 0 ? _eval.Pop() : null;
                        if (_callStack.Count == 0)
                        {
                            Finish();
                            return;
                        }
                        var frame = _callStack[^1];
                        _callStack.RemoveAt(_callStack.Count - 1);
                        _ip = frame.ReturnIp;
                        if (ret != null) _eval.Push(ret);
                        break;
                    }

                case OpCode.Sys:
                    {
                        var id = (SyscallId)instr.Operand;
                        HandleSyscall(id);
                        break;
                    }

                case OpCode.AllocObject:
                    {
                        var typeToken = _eval.Pop();
                        int typeId = Convert.ToInt32(typeToken);
                        var handle = _runtime.AllocObject(typeId);
                        _eval.Push(handle);
                        break;
                    }

                case OpCode.GetField:
                    {
                        var fieldName = (string)_code.Constants[instr.Operand]!;
                        var obj = _eval.Pop();
                        var val = _runtime.GetField(obj, fieldName);
                        _eval.Push(val);
                        break;
                    }

                case OpCode.SetField:
                    {
                        var fieldName = (string)_code.Constants[instr.Operand]!;
                        var value = _eval.Pop();
                        var obj = _eval.Pop();
                        _runtime.SetField(obj, fieldName, value);
                        break;
                    }

                case OpCode.Yield:
                    // cooperative yield: stop stepping this thread this tick
                    _thread.IsYielded = true;
                    break;

                case OpCode.Halt:
                    Finish();
                    break;

                default:
                    throw new NotSupportedException($"Opcode not implemented: {instr.OpCode}");
            }
        }

        private void Finish()
        {
            IsFinished = true;
            _thread.IsFinished = true;
        }

        private static object Arithmetic(OpCode op, object? a, object? b)
        {
            if (a is int ai && b is int bi)
            {
                return op switch
                {
                    OpCode.Add => ai + bi,
                    OpCode.Sub => ai - bi,
                    OpCode.Mul => ai * bi,
                    OpCode.Div => bi == 0 ? 0 : ai / bi,
                    _ => 0
                };
            }

            if (a is double ad || b is double bd)
            {
                double da = Convert.ToDouble(a ?? 0);
                double db = Convert.ToDouble(b ?? 0);
                return op switch
                {
                    OpCode.Add => da + db,
                    OpCode.Sub => da - db,
                    OpCode.Mul => da * db,
                    OpCode.Div => db == 0 ? 0.0 : da / db,
                    _ => 0.0
                };
            }

            // string concat for Add
            if (op == OpCode.Add)
                return $"{a}{b}";

            return 0;
        }

        private static bool Compare(OpCode op, object? left, object? right)
        {
            int cmp = Comparer<object?>.Default.Compare(left, right);
            return op switch
            {
                OpCode.CompareEqual => Equals(left, right),
                OpCode.CompareNotEqual => !Equals(left, right),
                OpCode.CompareLess => cmp < 0,
                OpCode.CompareLessEqual => cmp <= 0,
                OpCode.CompareGreater => cmp > 0,
                OpCode.CompareGreaterEqual => cmp >= 0,
                _ => false
            };
        }

        private void HandleSyscall(SyscallId id)
        {
            switch (id)
            {
                case SyscallId.Print:
                    {
                        var arg = _eval.Pop();
                        _runtime.SysPrint(arg?.ToString() ?? "");
                        break;
                    }

                case SyscallId.Log:
                    {
                        var arg = _eval.Pop();
                        _runtime.SysLog(arg?.ToString() ?? "");
                        break;
                    }

                case SyscallId.SpriteCenter:
                    {
                        var spriteHandle = _eval.Pop();
                        _runtime.CenterSprite(spriteHandle);
                        break;
                    }

                case SyscallId.SpriteGlide:
                    {
                        var y = Convert.ToDouble(_eval.Pop() ?? 0.0);
                        var x = Convert.ToDouble(_eval.Pop() ?? 0.0);
                        var duration = Convert.ToDouble(_eval.Pop() ?? 0.0);
                        var spriteHandle = _eval.Pop();
                        _runtime.GlideSprite(spriteHandle, duration, x, y);
                        break;
                    }

                case SyscallId.WaitMs:
                    {
                        var ms = Convert.ToInt32(_eval.Pop() ?? 0);
                        _thread.IsWaiting = true;
                        _thread.WakeAt = DateTime.UtcNow.AddMilliseconds(ms);
                        break;
                    }

                case SyscallId.Broadcast:
                    {
                        var message = _eval.Pop()?.ToString() ?? "";
                        _runtime.Broadcast(message, _thread);
                        break;
                    }

                case SyscallId.BroadcastAndWait:
                    {
                        var message = _eval.Pop()?.ToString() ?? "";
                        _runtime.BroadcastAndWait(message, _thread);
                        break;
                    }

                case SyscallId.ExitThread:
                    {
                        Finish();
                        break;
                    }

                default:
                    // For unimplemented syscalls, log and continue
                    _runtime.SysLog($"Unimplemented syscall: {id}");
                    break;
            }
        }

        private static bool IsTruthy(object? v)
        {
            if (v == null) return false;
            if (v is bool b) return b;
            if (v is int i) return i != 0;
            if (v is double d) return Math.Abs(d) > double.Epsilon;
            if (v is string s) return s.Length > 0;
            return true;
        }

        private sealed class CallFrame
        {
            public int ReturnIp { get; }
            public CallFrame(int returnIp) => ReturnIp = returnIp;
        }
    }

    // small extension methods for stack
    internal static class StackExtensions
    {
        public static void Push<T>(this Stack<T> s, T v) => s.Push(v);
        public static T Pop<T>(this Stack<T> s)
        {
            var v = s.Peek();
            s.Pop();
            return v;
        }
    }
}
