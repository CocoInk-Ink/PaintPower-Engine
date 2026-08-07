using System;
using System.Collections.Generic;
using Toolbox.Display.DisplayIntegration;

namespace PaintPower_VM.VM.Runtime.Primitives.JavaScriptPrims;

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