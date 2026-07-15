// File: PaintPower.Vm.Runtime.Interpreter/Interpreter.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintPower.Vm.Runtime.Ksa;
using PaintPower.Vm;
using PaintPower.Vm.Runtime.Sprites;
using PaintPower.Display.DisplayIntegration;

namespace PaintPower.Vm.Runtime.Interpreter
{
    public sealed class Interpreter
    {
        private readonly Bytecode _code;
        private int _ip;
        private Stack<object?> _eval = new();
        private readonly List<CallFrame> _callStack = new();
        private readonly object?[] _locals;
        private readonly VmThread _thread; // Parent.
        private readonly RuntimeBridge _runtime;

        // It's "diplay" don't you dare change it. "DisplayIntegration Item" (DII),
        // An item for the "DisplayIntegration Player" (DIPlay/Diplay)
        private readonly DIItem diplay_item;

        public bool IsFinished { get; private set; }
        public Primitives.Primitives primitives = new();
        public Dictionary<OpCode, Func<Stack<object?>, OpCode, int, DIItem, object?>> functions = new();

        public Interpreter(Bytecode code, VmThread thread, RuntimeBridge runtime, DIItem diplay_item)
        {
            _code = code ?? throw new ArgumentNullException(nameof(code));
            _thread = thread ?? throw new ArgumentNullException(nameof(thread));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _locals = new object?[code.LocalCount];
            _ip = 0;
            IsFinished = false;

            this.diplay_item = diplay_item;

            primitives.addPrimsTo(functions);
        }

        public void addPrimsTo() {}

        public void Step()
        {
            if (IsFinished) return;
            if (_ip < 0 || _ip >= _code.Instructions.Length)
            {
                Finish();
                return;
            }

            var instr = _code.Instructions[_ip++];
            var function = functions[instr.OpCode];

            function(_eval, instr.OpCode, instr.Operand, sprite);

        }

        private void Finish()
        {
            IsFinished = true;
            _thread.IsFinished = true;
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
