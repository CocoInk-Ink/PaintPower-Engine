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

            var vm = new Vm.Vm();

            var diplay = new DIItem
            {
                x = 0,
                y = 0,
                IsVisible = true
            };

            // Build program
            var code = new List<Instruction>
            {
                // x = 3
                new Instruction("mem:declareVar", new() { "x", "int", true }),
                new Instruction("mem:setVar",     new() { "x", 7 }),

                // y = 7
                new Instruction("mem:declareVar", new() { "y", "int", true }),
                new Instruction("mem:setVar",     new() { "y", 7 }),

                // z declared
                new Instruction("mem:declareVar", new() { "z", "int", true }),

                // ---------------------------------------------
                // Define function add(a, b)
                // ---------------------------------------------
                new Instruction("mem:method:declare", new()
                {
                    "add",                          // name
                    new List<string> { "a", "b" },  // parameters
                    "int",                          // return type
                    new List<Instruction>           // body
                    {
                        // return a + b
                        new Instruction("ret", new()
                        {
                            new Instruction("math:add", new()
                            {
                                new Instruction("mem:getVar", new() { "a" }),
                                new Instruction("mem:getVar", new() { "b" })
                            })
                        })
                    }
                }),

                // ---------------------------------------------
                // z = add(x, y)
                // ---------------------------------------------
                new Instruction("mem:setVar", new()
                {
                    "z",
                    new Instruction("mem:method:call", new()
                    {
                        "add",
                        new Instruction("mem:getVar", new() { "x" }),
                        new Instruction("mem:getVar", new() { "y" })
                    })
                }),

                new Instruction("halt", null)
            };

            var thread = new VmThread();
            vm.AddThread(thread);

            thread.Load(vm, code, diplay);

            while (!thread.IsFinished)
                await thread.Step();

            var x = thread.memory.GetValue("x");
            var y = thread.memory.GetValue("y");
            var z = thread.memory.GetValue("z");

            Console.WriteLine($"x = {x}");
            Console.WriteLine($"y = {y}");
            Console.WriteLine($"z = {z}");

            Console.WriteLine("=== VM Test End ===");
        }
    }
}
