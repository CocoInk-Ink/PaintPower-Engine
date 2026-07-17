// Memory.cs
using System;
using System.Collections.Generic;

namespace PaintPower.Vm.Processing.memory;

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

        slot.Item.Value = value;
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
}
