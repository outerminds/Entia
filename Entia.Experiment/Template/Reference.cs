using System;
using Entia.Initializers;
using Entia.Instantiators;

namespace Entia.Modules.Template
{
    public readonly struct Reference : IEquatable<Reference>
    {
        public readonly int Index;
        public readonly IInitializer Initializer;
        public readonly IInstantiator Instantiator;

        public Reference(int index, (IInstantiator instantiator, IInitializer initializer) pair) : this(index, pair.instantiator, pair.initializer) { }
        public Reference(int index, IInstantiator instantiator, IInitializer initializer)
        {
            Index = index;
            Initializer = initializer;
            Instantiator = instantiator;
        }

        public bool Equals(Reference other) => (Index, Initializer, Instantiator) == (other.Index, other.Initializer, other.Instantiator);
        public override bool Equals(object obj) => obj is Reference reference && Equals(reference);
        public override int GetHashCode() => (Index, Initializer, Instantiator).GetHashCode();
    }
}
