using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Vm.Runtime.Interpreter;
using PaintPower.Vm.Runtime.Ksa;
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    // Code effects who?
	public DIItem target;

	public Dictionary<string, object?> memory; // Passed by a VM, don't init.

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.


    public void LoadBytecode(Bytecode code, Vm vm, DIItem target)
    {
        var runtime = new RuntimeBridge(vm);
        _interpreter = new Interpreter(code, this, runtime, target);
        if (target == null) return;
        this.target = target;
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
