using System;
using System.Collections.Generic;
using Toolbox.Display.DisplayIntegration;
using PaintPower_VM.VM.Processing.memory.Variables;

namespace PaintPower_VM.VM.Runtime.Primitives;

public class AsyncPrimitives
{

	private VmThread thread;

	public AsyncPrimitives(VmThread thread)
	{
		this.thread = thread;
	}

	public void addPrimsTo(Dictionary<string, Func<string, List<object?>?, DIItem, object?>> primTable)
	{
		primTable["async:call"] = AsyncCall;
		primTable["await"] = PrimAwait;
	}

	private object? AsyncCall(string op, List<object?>? args, DIItem item)
	{
		string name = (string)args![0]!;

		// Resolve function
		var slot = thread.ResolveVariable(name);
		if (slot.Item.Value is not VmFunction fn)
			throw new Exception($"'{name}' is not a function.");

		// Create a new thread for the async function
		var asyncThread = new VmThread();
		thread.parent.AddThread(asyncThread);
		asyncThread.parent = thread.parent; // Add the parent

		// Create a future
		var future = new VmFuture(asyncThread);
		asyncThread.Future = future;

		// Build the function body as code for the new thread
		asyncThread.Load(thread.parent, fn.Body, item);

		// Bind parameters
		asyncThread.EnterScope();
		for (int i = 0; i < fn.ParameterNames.Count; i++)
		{
			string paramName = fn.ParameterNames[i];
			object? val = PrimitiveHelpers.Eval(args[i + 1], thread, item);

			asyncThread.DeclareVariable(paramName, new VarType("auto"), true, false);
			asyncThread.memory.PushValue(paramName, val);
		}

		// Return the future immediately
		return future;
	}

	private object? PrimAwait(string op, List<object?>? args, DIItem item)
	{
		var future = PrimitiveHelpers.Eval(args![0], thread, item) as VmFuture;
		if (future == null)
			throw new Exception("await expects a future");

		if (!future.IsCompleted)
		{
			thread.IsWaiting = true;
			thread.WakeAt = null; // wait indefinitely
			thread.AwaitingFuture = future;
			return null;
		}

		return future.Result;
	}
}