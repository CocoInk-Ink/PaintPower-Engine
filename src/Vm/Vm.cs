using System;
using PaintPower.Logging;
using PaintPower.Dialogs;
using PaintPower.Editors;
using PaintPower.FileExplorer;
using PaintPower.Networking;
using PaintPower.ProjectSystem;
using PaintPower.SpriteEditor;
using PaintPower.Time;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaintPower.Compiler.PreBytecode;

using PaintPower.Sprites;

namespace PaintPower.Vm;

// VM runtime, runtime for the project player virtual machine.

public class Vm
{

    public static Vm? vm;
    public string Id { get; set; }

#pragma warning disable IDE0044 // Add readonly modifier
    private List<string> IdList = new();

#pragma warning restore IDE0044 // Add readonly modifier
    public int threadsCount = 0;

    public int currentThread = 0;

    // If 3D processing is enabled, use the system's processing power to run the VM. Use the GPU for 3D Acceleration.
    public bool usingSystemProcessing = false;

    public Dictionary<string, VmThread> Threads { get; } = new();

    // For embedded VMs.
    public Dictionary<string, Vm> VMs { get; } = new();

#pragma warning disable
    public Vm()
    {
        try
        {
            Id = CreateId();
            // KiteScriptTest.Run();
        }
        catch (Exception e)
        {
            Log.QuickLog(e);
        }
        ;
        vm = this;
    }

    public static bool isThreadSafe(VmThread? thread) { 
        return thread.isPaused == false;
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
        foreach(Vm vm in VMs.Values)
        {
            await vm.Tick();
        }

        foreach (VmThread thread in Threads.Values)
        {
            // Check if thread is valid:

            if (!isThreadSafe(thread)) continue;
            await thread.Step();

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                MainWindow.window.InvalidateVisual();
            });

        }
    }
}