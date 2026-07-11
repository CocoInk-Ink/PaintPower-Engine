// File: PaintPower.Runtime.ObjectModel/VmObject.cs
using System;
using System.Collections.Generic;

namespace PaintPower.Runtime.ObjectModel
{
    public sealed class VmObject
    {
        public TypeDescriptor Type { get; }
        private readonly object?[] _fields;

        public VmObject(TypeDescriptor type)
        {
            Type = type;
            _fields = new object?[type.FieldCount];
        }

        public object? GetField(int index)
        {
            return _fields[index];
        }

        public void SetField(int index, object? value)
        {
            _fields[index] = value;
        }
    }
}
