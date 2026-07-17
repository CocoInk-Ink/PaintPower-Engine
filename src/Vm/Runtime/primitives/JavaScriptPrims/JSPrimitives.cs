using System;
using System.Collections.Generic;
using PaintPower.Display.DisplayIntegration;

namespace PaintPower.Vm.Runtime.Primitives.JavaScriptPrims;

public class JSPrimitives
{
	private readonly VmThread thread;

	public JSPrimitives(VmThread vmThread)
	{
		thread = vmThread;
	}
	public void addPrimsTo(Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable)
	{
		// Add prims here.
	}
}