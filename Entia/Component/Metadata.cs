using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Linq;

namespace Entia.Modules.Component
{
    /// <summary>
    /// Holds some metadata about a component type.
    /// </summary>
    [ThreadSafe]
    public readonly struct Metadata : IEquatable<Metadata>
    {
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(in Metadata a, in Metadata b) => a.Equals(b);
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(in Metadata a, in Metadata b) => !a.Equals(b);

        /// <summary>
        /// Returns true if the instance is valid.
        /// </summary>
        /// <value>Returns <c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid => Data != null && Index >= 0;
        /// <summary>
        /// The component type.
        /// </summary>
        public Type Type => Data.Type;

        /// <summary>
        /// The component type data.
        /// </summary>
        public readonly TypeData Data;
        /// <summary>
        /// The component index.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Metadata"/> struct.
        /// </summary>
        public Metadata(TypeData data, int index)
        {
            Data = data;
            Index = index;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(Metadata other) => (Data, Index) == (other.Data, other.Index);
        /// <inheritdoc cref="ValueType.Equals(object)"/>
        public override bool Equals(object obj) => obj is Metadata metadata && Equals(metadata);
        /// <inheritdoc cref="ValueType.GetHashCode"/>
        public override int GetHashCode() => (Data, Index).GetHashCode();
    }
}