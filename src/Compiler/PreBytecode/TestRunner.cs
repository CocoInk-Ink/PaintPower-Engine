using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaintPower.Vm;
using PaintPower.Vm.Runtime.Interpreter;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Logging;
using PaintPower.Vm.Runtime;

namespace PaintPower.Compiler.PreBytecode
{
    public static class TestRunner
    {
        public static async Task RunTest()
        {
            Console.WriteLine("=== VM Test Start ===");

            // Create VM
            var vm = new Vm.Vm();

            // Create a fake DIItem (VM requires one)
            var diplay = new DIItem
            {
                x = 0,
                y = 0,
                IsVisible = true
            };

            // Build a tiny prim program
            var code = new List<Instruction>
            {
                new Instruction("mem:declareVar", new() { "x", "int", true }),
                new Instruction("mem:setVar",     new() { "x", 3 }),

                new Instruction("mem:declareVar", new() { "y", "int", true }),
                new Instruction("mem:setVar",     new() { "y", 7 }),

                new Instruction("mem:declareVar", new() { "z", "int", true }),

                // z = x + y

                // Layer 1
                new Instruction("mem:setVar",     
                    // Layer 2
                    new() { "z", new Instruction("math:add",       
                        // Layer 3
                        new() { 
                            new Instruction("mem:getVar", 
                                new() { "x" }), 
                            new Instruction("mem:getVar", 
                                new() { "y" }) 
                        }
                    ) }
                ),

                new Instruction("halt", null)
            };

            // Create thread
            var thread = new VmThread();
            vm.AddThread(thread);

            // Load program
            thread.Load(vm, code, diplay);

            // Run until finished
            while (!thread.IsFinished)
            {
                await thread.Step();
            }

            // Check result
            var x = thread.memory.GetValue("x");
            var y = thread.memory.GetValue("y");
            var z = thread.memory.GetValue("z");

            Console.WriteLine($"x = {x}");
            Console.WriteLine($"y = {y}");
            Console.WriteLine($"z = {z}");

            if (Convert.ToInt32(z) != 10)
                throw new InvalidOperationException($"Expected z = 10, got {z}");

            Console.WriteLine("=== VM Test End ===");
        }
    }
}
