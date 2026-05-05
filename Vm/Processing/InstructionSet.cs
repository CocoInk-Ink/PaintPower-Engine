using System.Collections.Generic;
using PaintPower.Runtime.Bytecode;

namespace PaintPower.Vm.Processing;

public class InstructionSet
{
    private readonly List<Instruction> _instructions = new();
    private readonly List<object> _constants = new();
    private int _localCount = 0;

    public int AddConstant(object value)
    {
        _constants.Add(value);
        return _constants.Count - 1;
    }

    public int AddLocal()
    {
        return _localCount++;
    }

    public void Emit(OpCode op, int operand = 0)
    {
        _instructions.Add(new Instruction(op, operand));
    }

    public Bytecode ToBytecode()
    {
        return new Bytecode(
            _instructions.ToArray(),
            _constants.ToArray(),
            _localCount
        );
    }

    public void EmitPrint()
    {
        Emit(OpCode.Print);
    }

}
