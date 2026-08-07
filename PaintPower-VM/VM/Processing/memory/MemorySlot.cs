// MemorySlot.cs

using System;

namespace PaintPower_VM.VM.Processing.memory;

public class MemorySlot
{
    public string SlotId { get; }
    public MemoryItem Item { get; }

    public MemorySlot(string slotId, MemoryItem? item = null)
    {
        SlotId = slotId;
        Item = item ?? new MemoryItem();
    }
}