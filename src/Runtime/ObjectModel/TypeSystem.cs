// File: PaintPower.Runtime.ObjectModel/TypeSystem.cs
using System.Collections.Generic;

namespace PaintPower.Runtime.ObjectModel
{
    public sealed class TypeSystem
    {
        private readonly Dictionary<int, TypeDescriptor> _types = new();

        public void Register(TypeDescriptor desc)
        {
            _types[desc.TypeId] = desc;
        }

        public TypeDescriptor Get(int id)
        {
            return _types[id];
        }
    }
}
