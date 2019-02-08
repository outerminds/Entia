using Entia.Core;
using Entia.Core.Documentation;
using System;
using System.Linq;
using System.Reflection;

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
        public bool IsValid => Type != null && Index >= 0;

        /// <summary>
        /// The component type.
        /// </summary>
        public readonly Type Type;
        /// <summary>
        /// The component index.
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// Is <c>true</c> if the component type has recusively no references to managed types; otherwise, <c>false</c>.
        /// </summary>
        public readonly bool IsPlain;

        /// <summary>
        /// Initializes a new instance of the <see cref="Metadata"/> struct.
        /// </summary>
        public Metadata(Type type, int index)
        {
            Type = type;
            Index = index;
            IsPlain = TypeUtility.IsPlain(type);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(Metadata other) => (Type, Index) == (other.Type, other.Index);
        /// <inheritdoc cref="ValueType.Equals(object)"/>
        public override bool Equals(object obj) => obj is Metadata metadata && Equals(metadata);
        /// <inheritdoc cref="ValueType.GetHashCode"/>
        public override int GetHashCode() => (Type, Index).GetHashCode();
    }
}