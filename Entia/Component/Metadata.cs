using System;
using System.Reflection;
using System.Linq;
using Entia.Core;

namespace Entia.Modules.Component
{
    public readonly struct Metadata : IEquatable<Metadata>
    {
        public static bool operator ==(in Metadata data1, in Metadata data2) => data1.Equals(data2);
        public static bool operator !=(in Metadata data1, in Metadata data2) => !data1.Equals(data2);

        public static readonly Metadata Invalid = new Metadata(null, -1, new BitMask(), Array.Empty<FieldInfo>());

        public bool IsValid => Type != null && Index >= 0 && Mask != null && Fields != null;
        public bool IsEmpty => Fields?.Length == 0;

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

        public bool Equals(Metadata other) =>
            (Type, Index, Mask) == (other.Type, other.Index, other.Mask) &&
            (Fields == other.Fields || Fields.SequenceEqual(other.Fields));
        public override bool Equals(object obj) => obj is Metadata metadata && Equals(metadata);
        public override int GetHashCode() => (Type, Index, Mask).GetHashCode() ^ ArrayUtility.GetHashCode(Fields);
    }
}