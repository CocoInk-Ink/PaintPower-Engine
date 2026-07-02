// File: PaintPower.Runtime.Ksa/OpCode.cs
namespace PaintPower.Runtime.Ksa
{
    public enum OpCode : int
    {
        Nop = 0,
        // stack / constants
        PushConst,
        PushNull,
        LoadLocal,
        StoreLocal,
        // arithmetic / logic
        Add,
        Sub,
        Mul,
        Div,
        CompareEqual,
        CompareNotEqual,
        CompareLess,
        CompareLessEqual,
        CompareGreater,
        CompareGreaterEqual,
        // control flow
        Jump,
        JumpIfFalse,
        Call,       // direct call to IP
        Ret,
        // objects / fields
        AllocObject,
        GetField,   // operand = constant index (field name)
        SetField,   // operand = constant index (field name)
        // syscalls
        Sys,        // operand = syscall id (int)
        // thread / scheduling
        Yield,
        Halt,
        // convenience
        LoadConstIndex, // alias for PushConst (keeps old names)
    }

    public readonly struct Instruction
    {
        public OpCode OpCode { get; }
        public int Operand { get; }

        public Instruction(OpCode op, int operand = 0)
        {
            OpCode = op;
            Operand = operand;
        }
    }

    public sealed class Bytecode
    {
        public Instruction[] Instructions { get; }
        public object[] Constants { get; }
        public int LocalCount { get; }

        public Bytecode(Instruction[] instructions, object[] constants, int localCount)
        {
            Instructions = instructions;
            Constants = constants;
            LocalCount = localCount;
        }
    }
}
