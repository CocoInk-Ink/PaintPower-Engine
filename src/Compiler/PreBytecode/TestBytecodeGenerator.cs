// File: PaintPower.Compiler.PreBytecode/TestBytecodeGenerator.cs
using PaintPower.Vm.Processing;
using PaintPower.Runtime.Ksa;

namespace PaintPower.Compiler.PreBytecode
{
    public static class TestBytecodeGenerator
    {
        public static Bytecode CreateOopTestProgram()
        {
            var set = new InstructionSet();

            // Constants
            int typeId = set.AddConstant(1); // Our test type
            int fieldName = set.AddConstant("value");
            int methodName = set.AddConstant("PrintValue");
            int number = set.AddConstant(42);

            // Allocate object
            set.Emit(OpCode.PushConst, typeId);
            set.Emit(OpCode.AllocObject);

            // Duplicate object reference (simple trick: store in local)
            int localObj = set.AddLocal();
            set.Emit(OpCode.StoreLocal, localObj);

            // Set field "value" = 42
            set.Emit(OpCode.LoadLocal, localObj);
            set.Emit(OpCode.PushConst, number);
            set.Emit(OpCode.SetField, fieldName);

            // Call method PrintValue()
            set.Emit(OpCode.LoadLocal, localObj);
            set.Emit(OpCode.CallMethod, methodName);

            // Halt
            set.Emit(OpCode.Halt);

            return set.ToBytecode();
        }
    }
}
