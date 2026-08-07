// Memory.cs
using System;
using System.Collections.Generic;
using PaintPower_VM.VM.Processing.memory.Variables;

namespace PaintPower_VM.VM.Processing.memory;

public class Memory
{
	public int Limit { get; }
	public Dictionary<string, MemorySlot> MemorySlots { get; } = new();

	public Memory(int slots)
	{
		Limit = slots;
	}

	public int SlotsLeft => Limit - MemorySlots.Count;
	public bool AnySlotsAvailable => SlotsLeft > 0;

	public bool IsAssigned(string key) => MemorySlots.ContainsKey(key);

	public void PushValue(string slotId, object? value)
	{
		if (!AnySlotsAvailable && !IsAssigned(slotId))
			throw new OutOfMemoryException("No more memory is available.");

		if (!MemorySlots.TryGetValue(slotId, out var slot))
		{
			slot = new MemorySlot(slotId);
			MemorySlots[slotId] = slot;
		}

		var item = slot.Item;

		// If this is a Variable, enforce readonly rules
		if (item is Variable variable)
		{
			// readonly: can only assign once
			if (variable.IsReadonly && variable.HasBeenAssigned)
				throw new InvalidOperationException(
					$"Memory slot {slotId} is readonly and cannot be assigned again.");

			// mutable: normal rule
			if (!variable.IsMutable)
				throw new InvalidOperationException(
					$"Memory slot {slotId} is immutable.");

			// type enforcement
			if (variable.Type != null)
			{
				if (!TypeMatches(variable.Type, value))
					throw new InvalidOperationException(
						$"Type mismatch: expected {variable.Type.Name}, got {value?.GetType().Name}");
			}

			// assignment allowed
			variable.Value = value;
			variable.HasBeenAssigned = true;
			return;
		}

		// Non-variable MemoryItem (raw)
		if (!item.IsMutable)
			throw new InvalidOperationException($"Memory slot {slotId} is immutable.");

		if (item.Type != null)
		{
			if (!TypeMatches(item.Type, value))
				throw new InvalidOperationException(
					$"Type mismatch: expected {item.Type.Name}, got {value?.GetType().Name}");
		}

		item.Value = value;
	}

	public object? GetValue(string slotId)
	{
		return MemorySlots.TryGetValue(slotId, out var slot)
			? slot.Item.Value
			: null;
	}

	public void FreeSlot(string slotId)
	{
		MemorySlots.Remove(slotId);
	}

	private bool TypeMatches(VarType type, object? value)
	{
		if (value == null) return true; // allow null for now

		return type.Name switch
		{
			"int" => value is int,
			"float" => value is float || value is double,
			"double" => value is double,
			"string" => value is string,
			"object" => value is VmObject,
			_ => true // custom types allowed
		};
	}

}