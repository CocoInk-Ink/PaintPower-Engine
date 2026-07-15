// File: PaintPower.Compiler.PreBytecode/TestRunner.cs
using System.Threading.Tasks;
using PaintPower.Vm;
using PaintPower.Logging;
using PaintPower.Vm.Runtime.Ksa;

namespace PaintPower.Compiler.PreBytecode
{
    public static class TestRunner
    {
        public static async Task RunPrintTestAsync()
        {
            Log.QuickLog("Starting VM print test...");

            // 1) Build bytecode
            Bytecode bc = TestBytecodeGenerator.CreateOopTestProgram();

            // 2) Create a VM and a thread
            var vm = new Vm.Vm();
            var thread = new VmThread();

            // 3) Add thread to VM (so Tick and runtime lookups work if needed)
            vm.AddThread(thread);

            // 4) Load bytecode into the thread (uses the LoadBytecode(Bytecode, Vm) signature)
            thread.LoadBytecode(bc, vm);

            // 5) Run the thread until it pauses/finishes
            while (!thread.isPaused && !thread.IsFinished)
            {
                await thread.Step();
            }

            Log.QuickLog("VM print test finished.");
        }
    }
}
