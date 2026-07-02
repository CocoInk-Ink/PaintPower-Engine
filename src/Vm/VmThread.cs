using System;
using System.Threading.Tasks;
using PaintPower.Runtime.Interpreter;
using PaintPower.Runtime.Ksa;
using PaintPower.Vm.Processing;

namespace PaintPower.Vm;

public class VmThread
{
    public bool isPaused = false;

    public InstructionSet InstructionSet { get; set; } = new();
    private Interpreter? _interpreter;

    public bool IsWaiting = false;
    public DateTime? WakeAt = null;
    public bool IsYielded = false;
    public bool IsFinished = false;
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public void LoadBytecode(Bytecode code, Vm vm)
    {
        var runtime = new RuntimeBridge(vm);
        _interpreter = new Interpreter(code, this, runtime);
    }

    public async Task Step()
    {
        if (_interpreter == null || isPaused || IsWaiting || IsYielded || IsFinished)
            return;

        _interpreter.Step();

        // reset yield flag so next tick can run again
        IsYielded = false;

        if (_interpreter.IsFinished)
            isPaused = true;
    }
}
