using System;
using System.Collections.Generic;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Vm.Runtime.Ksa;

using PaintPower.Vm.Runtime.Sprites;

namespace PaintPower.Vm.Runtime.Primitives.SpritePrims;

public class SpriteMovePrims
{
	public void addPrimsTo(Dictionary<string, Func<Stack<object?>, OpCode, int, DIItem, object?>> primTable)
	{
		primTable["high:spriteMove"] = setSpritePos;
	}

	public object? setSpritePos(Stack<object?> _eval, OpCode op, int operand, DIItem item)
	{
		return new OutOfMemoryException();
	}
}