using System;
using System.Collections.Generic;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Vm.Runtime.Ksa;
using PaintPower.Vm.Runtime.Sprites;

namespace PaintPower.Vm.Runtime.Primitives.SpritePrims;

public class SpritePrimitives
{
	public void addPrimsTo(Dictionary<string, Func<Stack<object?>, OpCode, int, DIItem, object?>> primTable)
	{
		new SpriteMovePrims().addPrimsTo(primTable);
	}
}