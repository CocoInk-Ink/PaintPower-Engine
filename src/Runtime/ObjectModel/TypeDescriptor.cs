// File: PaintPower.Runtime.ObjectModel/TypeDescriptor.cs
using System.Collections.Generic;

namespace PaintPower.Runtime.ObjectModel
{
    public sealed class TypeDescriptor
    {
        public int TypeId { get; set; }
        public string Name { get; set; }

        public Dictionary<string, int> FieldMap { get; } = new();
        public Dictionary<string, int> MethodMap { get; } = new();

        public int FieldCount => FieldMap.Count;
    }
}
