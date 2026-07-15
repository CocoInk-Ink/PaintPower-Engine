// VarType.cs

using System;

namespace PaintPower.Vm.Processing.memory.Variables;

public class VarType
{
	// The general stuff
	public static string[] GeneralBuiltins = { "int", "float", "double", "short", "long", "byte", "char", "array", "object" };
	public MemorySlot? definition;
}