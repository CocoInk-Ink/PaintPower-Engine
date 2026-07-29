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
                new Instruction("mem:setVar",     new() { "x", 3 }),

                // y = 7
                new Instruction("mem:declareVar", new() { "y", "int", true }),
                new Instruction("mem:setVar",     new() { "y", 7 }),

                // z declared
                new Instruction("mem:declareVar", new() { "z", "int", true }),

                // async function asyncAdd(a, b)
                new Instruction("mem:method:declare", new()
                {
                    "asyncAdd",
                    new List<string> { "a", "b" },
                    "int",
                    new List<Instruction>
                    {
                        new Instruction("yield", null),
                        new Instruction("return", new()
                        {
                            new Instruction("math:add", new()
                            {
                                new Instruction("mem:getVar", new() { "a" }),
                                new Instruction("mem:getVar", new() { "b" })
                            })
                        })
                    }
                }),

                // f = asyncAdd(x, y)
                new Instruction("mem:declareVar", new() { "f", "future", true }),
                new Instruction("mem:setVar", new()
                {
                    "f",
                    new Instruction("async:call", new()
                    {
                        "asyncAdd",
                        new Instruction("mem:getVar", new() { "x" }),
                        new Instruction("mem:getVar", new() { "y" })
                    })
                }),

                // z = await f
                new Instruction("mem:setVar", new()
                {
                    "z",
                    new Instruction("await", new()
                    {
                        new Instruction("mem:getVar", new() { "f" })
                    })
                }),

                new Instruction("halt", null)
            };

            var thread = new VmThread();
            vm.AddThread(thread);

            thread.Load(vm, code, diplay);

            vm.AddThread(thread);

            while (!vm.AllThreadsStopped())
                await vm.Tick();

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
