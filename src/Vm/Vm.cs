using System;
using PaintPower.Logging;
using PaintPower.Dialogs;
using PaintPower.FileEditors;
using PaintPower.FileExplorer;
using PaintPower.Networking;
using PaintPower.ProjectSystem;
using PaintPower.ProjectSystem.SpriteEditor;
using PaintPower.Time;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaintPower.Compiler.PreBytecode;

using PaintPower.Vm.Runtime.Sprites;
using PaintPower.VMPanel;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Vm.Runtime.ObjectModel;

namespace PaintPower.Vm;

// VM runtime, runtime for the project player virtual machine.

public class Vm
{

    public static Vm? vm;

    public static bool isProjectLoading = false;

    // Memory!
    // Use a string instead of a number, it's more versatile.

    // Example: 0xFFF, 0x5934
    public Dictionary<string, object?> memory;

    // =======================
    // Project Loading:
    // =======================
    public static async Task LoadProject(PaintProject project, DIPlay display)
{
    isProjectLoading = true;
    LoadSpritesIntoDisplay(project, display);
    isProjectLoading = false;
}

    // =======================
    // Virtual Machine Parts:
    // =======================

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
        }
        catch (Exception e)
        {
            Log.QuickLog(e);
        }
        ;

        memory = new(); // Add memory, note for next time you open this Nino!
    }

    public static bool isThreadSafe(VmThread? thread)
    {
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

    public static void LoadSpritesIntoDisplay(PaintProject project, DIPlay display)
    {
        if (project == null) return;
        if (display == null) return;

        display.items.Clear();

        foreach (var sprite in project.Sprites)
            display.items.Add(sprite.ToRuntimeSprite());
    }

    public async Task Tick()
    {

        if (isProjectLoading) return; // Make sure project is not loading! (Pasted multiple times for good measure.)
        foreach (Vm vm in VMs.Values)
        {
            if (isProjectLoading) return; // Make sure project is not loading! (Pasted multiple times for good measure.)
            await vm.Tick();
        }

        foreach (VmThread thread in Threads.Values)
        {
            // Check if thread is valid:

            if (isProjectLoading) return; // Make sure project is not loading! (Pasted multiple times for good measure.)

            if (!isThreadSafe(thread)) continue;
            await thread.Step();

        }
    }
}