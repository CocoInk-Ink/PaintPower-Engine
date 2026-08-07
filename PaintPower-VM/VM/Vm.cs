// PaintPower-VM/Vm/Vm.cs

using System;
using Toolbox.Logging;
using Toolbox.Time;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Toolbox.Display.Sprites;
using Toolbox.Display.DisplayIntegration;
using PaintPower_VM.VMPanel;
using PaintPower_VM.VM.Processing.memory;

namespace PaintPower_VM.VM;

// VM runtime, runtime for the project player virtual machine.

public class Vm
{

    public static Vm? vm;

    public static bool isProjectLoading = false;

    // Memory!
    // Use a string instead of a number, it's more versatile.

    // Example: 0xFFF, 0x5934
    public Memory memory;

    // =======================
    // Virtual Machine Parts:
    // =======================

    public string Id { get; set; }

#pragma warning disable IDE0044 // Add readonly modifier
    private List<string> IdList = new();

#pragma warning restore IDE0044 // Add readonly modifier
    public int threadsCount => Threads.Count;

    public int currentThread = 0;

    // If 3D processing is enabled, use the system's processing power to run the VM. Use the GPU for 3D Acceleration.
    public bool usingSystemProcessing = false;

    public Dictionary<string, VmThread> Threads { get; } = new();

    // For embedded VMs.
    public Dictionary<string, Vm> VMs { get; } = new();

    public bool AllThreadsStopped ()
    {
        int count = 0;
        foreach (VmThread thread in Threads.Values)
        {
            if (!isThreadSafe(thread)) count++;
        }
        return count == threadsCount;
    }

#pragma warning disable
    public Vm()
    {
        try
        {
            Id = CreateId();
        }
        catch (Exception e)
        {
            Log.QuickLog(e);
        }
        ;

        memory = new(256);
    }

    public static bool isThreadSafe(VmThread? thread)
    {
        return !(thread.isPaused || thread.IsWaiting || thread.IsFinished);
    }

#pragma warning restore

    public string CreateId(bool forThreads = true)
    {
        string id;
        do id = Guid.NewGuid().ToString();
        while (forThreads ? Threads.ContainsKey(id) : VMs.ContainsKey(id));
        return id;
    }

    public void AddThread(VmThread? thread, string? id = null)
    {
        id ??= CreateId(true);
        if (thread != null)
        {
            thread.memory = memory;
        }
        Threads[id] = thread!; // safe replace-or-add
    }

    public void AddVM(Vm? vm, string? id = null)
    {
        id ??= CreateId(false);

        if (vm != null) vm.Id = id;

        VMs[id] = vm!;
    }

    public void CreateNewThread()
    {
        AddThread(new VmThread());
    }

    public void CreateNewVm()
    {
        AddVM(new Vm());
    }

    public void RemoveThread(string id)
    {
        Threads.Remove(id);
    }

    public void RemoveVM(string id)
    {
        VMs.Remove(id);
    }

    public async Task Tick()
    {

        if (isProjectLoading) return; // Make sure project is not loading! (Pasted multiple times for good measure.)
        foreach (Vm vm in VMs.Values)
        {
            if (isProjectLoading) return; // Make sure project is not loading! (Pasted multiple times for good measure.)
            if (vm.AllThreadsStopped()) return;
            await vm.Tick();
        }

        foreach (VmThread thread in Threads.Values)
        {
            // Check if thread is valid:

            if (isProjectLoading) return; // Make sure project is not loading! (Pasted multiple times for good measure.)

            if (thread.IsWaiting && thread.AwaitingFuture?.IsCompleted == true)
            {
                thread.IsWaiting = false;
            }

            bool isSafe = isThreadSafe(thread);

            if (!isSafe) continue;

            await thread.Step();

        }
    }
}