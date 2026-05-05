using System.Threading.Tasks;
using PaintPower.Runtime.Bytecode;
using PaintPower.Runtime.Interpreter;
using PaintPower.Vm.Processing;

namespace PaintPower.Vm;

public class VmThread
{
    public bool isPaused = false;

    public InstructionSet InstructionSet { get; set; } = new();
    private Interpreter? _interpreter;

    public void LoadBytecode(Bytecode code)
    {
        _interpreter = new Interpreter(code);
    }

    public Task Step()
    {
        if (_interpreter == null || isPaused)
            return Task.CompletedTask;

        _interpreter.Step();

        if (_interpreter.IsFinished)
            isPaused = true;

        return Task.CompletedTask;
    }
}
