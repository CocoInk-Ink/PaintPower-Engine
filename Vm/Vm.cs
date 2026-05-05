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

namespace PaintPower.Vm;

// VM runtime, runtime for the project player virtual machine.

public class Vm
{

    public static Vm? vm;
    public string Id { get; private set; }

#pragma warning disable IDE0044 // Add readonly modifier
    private List<string> IdList = [];

#pragma warning restore IDE0044 // Add readonly modifier
    public int threadsCount = 0;

    public int currentThread = 0;

    // If 3D processing is enabled, use the system's processing power to run the VM.
    public bool usingSystemProcessing = false;

    public Dictionary<string, VmThread> Threads { get; } = new();

#pragma warning disable
    public Vm()
    {
        KiteScriptTest.Run();
        vm = this;
    }

    public static bool isThreadSafe(VmThread? thread)
        => thread is { isPaused: false };

#pragma warning restore

    public string CreateThreadId()
    {
        string id;
        do id = Guid.NewGuid().ToString();
        while (Threads.ContainsKey(id));
        return id;
    }

    public void AddThread(VmThread? thread, string? id = null)
    {
        id ??= CreateThreadId();
        Threads[id] = thread!; // safe replace-or-add
    }

    public void CreateNewThread()
    {
        AddThread(new VmThread());
    }

    public void RemoveThread(string id)
    {
        Threads.Remove(id);
    }

    public async Task Tick()
    {
        foreach (VmThread thread in Threads.Values)
        {
            // Check if thread is valid:

            if (!isThreadSafe(thread)) continue;
            await thread.Step();

        }
    }
}