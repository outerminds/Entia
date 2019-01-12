using System;
using System.Reflection;
using Entia.Core;

namespace Entia.Modules.Component
{
    public readonly struct Metadata
    {
        public bool IsValid => Type != null && Index >= 0 && Mask != null && Fields != null;
        public bool IsTag => Fields?.Length == 0;

        public readonly Type Type;
        public readonly int Index;
        public readonly BitMask Mask;
        public readonly FieldInfo[] Fields;

        public Metadata(Type type, int index, BitMask mask, FieldInfo[] fields)
        {
            Type = type;
            Index = index;
            Mask = mask;
            Fields = fields;
        }
    }
}