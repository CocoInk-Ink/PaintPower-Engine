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

namespace PaintPower.Vm;

// VM runtime, runtime for the project player virtual machine.

public class Vm
{
    public string Id { get; private set; }

    public int threadsCount = 0;

    public int currentThread = 0;

    // If 3D processing is enabled, use the system's processing power to run the VM.
    public bool usingSystemProcessing = false;

    #pragma warning disable
    public Vm(string id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
    }
}