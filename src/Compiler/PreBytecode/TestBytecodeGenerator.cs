// File: PaintPower.Compiler.PreBytecode/TestBytecodeGenerator.cs
using PaintPower.Vm.Processing;
using PaintPower.Runtime.Ksa;

namespace PaintPower.Compiler.PreBytecode
{
    public static class TestBytecodeGenerator
    {
        /// <summary>
        /// Build a tiny program:
        ///   PushConst "Hello from VM"
        ///   Sys Print
        ///   Halt
        /// </summary>
        public static Bytecode CreatePrintProgram()
        {
            var set = new InstructionSet();

            // Add the string constant
            int constIndex = set.AddConstant("Hello from VM");

            // Push the constant onto the VM stack
            set.Emit(OpCode.PushConst, constIndex);

            // Call syscall Print (operand is syscall id)
            set.Emit(OpCode.Sys, (int)PaintPower.Runtime.Ksa.SyscallId.Print);

            // End program
            set.Emit(OpCode.Halt);

            return set.ToBytecode();
        }
    }
}
