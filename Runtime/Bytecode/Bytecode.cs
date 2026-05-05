using System;

namespace PaintPower.Runtime.Bytecode;

public class Bytecode
{
    public Instruction[] Instructions { get; init; } = Array.Empty<Instruction>();
    public object[] Constants { get; init; } = Array.Empty<object>();
    public int LocalCount { get; init; } = 0;

    public Bytecode() {}

    public Bytecode(Instruction[] instructions, object[] constants, int localCount)
    {
        Instructions = instructions;
        Constants = constants;
        LocalCount = localCount;
    }
}
