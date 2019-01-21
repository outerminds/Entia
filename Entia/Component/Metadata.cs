using Entia.Core;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Modules.Component
{
    /// <summary>
    /// Holds some metadata about a component type.
    /// </summary>
    public readonly struct Metadata : IEquatable<Metadata>
    {
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(in Metadata data1, in Metadata data2) => data1.Equals(data2);
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(in Metadata data1, in Metadata data2) => !data1.Equals(data2);

        /// <summary>
        /// An invalid instance.
        /// </summary>
        public static readonly Metadata Invalid = new Metadata(null, -1, new BitMask(), Array.Empty<FieldInfo>());

        /// <summary>
        /// Returns true if the instance is valid.
        /// </summary>
        /// <value>Returns <c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid => Type != null && Index >= 0 && Mask != null && Fields != null;

        /// <summary>
        /// The component type.
        /// </summary>
        public readonly Type Type;
        /// <summary>
        /// The component index.
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// The component mask.
        /// </summary>
        public readonly BitMask Mask;
        /// <summary>
        /// The component fields.
        /// </summary>
        public readonly FieldInfo[] Fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="Metadata"/> struct.
        /// </summary>
        public Metadata(Type type, int index, BitMask mask, FieldInfo[] fields)
        {
            Type = type;
            Index = index;
            Mask = mask;
            Fields = fields;
        }


        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(Metadata other) =>
            (Type, Index, Mask) == (other.Type, other.Index, other.Mask) &&
            (Fields == other.Fields || Fields.SequenceEqual(other.Fields));
        /// <inheritdoc cref="ValueType.Equals(object)"/>
        public override bool Equals(object obj) => obj is Metadata metadata && Equals(metadata);
        /// <inheritdoc cref="ValueType.GetHashCode"/>
        public override int GetHashCode() => (Type, Index, Mask).GetHashCode() ^ ArrayUtility.GetHashCode(Fields);
    }
}