// Variable.cs

using System;

namespace PaintPower.Vm.Processing.memory.Variables;

public class Variable : MemoryItem
{
	public bool isMutable = true;
	public VarType? type;
}