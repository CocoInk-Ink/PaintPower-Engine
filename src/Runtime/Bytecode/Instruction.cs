namespace PaintPower.Runtime.Bytecode;

public readonly struct Instruction
{
    public OpCode OpCode { get; }
    public int Operand { get; }

    public Instruction(OpCode opCode, int operand = 0)
    {
        OpCode = opCode;
        Operand = operand;
    }
}
