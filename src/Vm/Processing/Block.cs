// Block.cs

using System;
using System.Collections.Generic;
using PaintPower.Vm.Processing.memory;
using PaintPower.Vm.Processing.memory.Variables;

namespace PaintPower.Vm.Processing;

public class ScopeBlock
{
	public List<ScopeBlock>? children;
	public List<Variable>? Variables;

}