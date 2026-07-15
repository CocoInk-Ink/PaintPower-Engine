using System;
using System.Collections.Generic;
using Tmds.DBus.Protocol;

namespace PaintPower.Vm.Processing.memory;

public class Memory
{
	public int limit = 0;
	public Dictionary<string, MemorySlot> MemorySlots = new();
	public bool isAssigned(string key) => MemorySlots.ContainsKey(key);
	public void PushValue(string slot, object? value)
	{
		if (limit == MemorySlots.Count) throw new OutOfMemoryException("No more memory is available.");
	}

	public int SlotsLeft => limit - MemorySlots.Count;
	public bool AnySlotsAvailable => SlotsLeft >= 1;

	public Memory(int slots)
	{
		limit = slots;
	}
}