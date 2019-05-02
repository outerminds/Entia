using Entia.Initializers;
using Entia.Instantiators;
using System;
using System.Collections.Generic;

namespace Entia.Modules.Template
{
    public readonly struct Context
    {
        public readonly struct Key : IEquatable<Key>
        {
            public readonly object Value;
            public Key(object value) { Value = value; }
            public bool Equals(Key other) => EqualityComparer<object>.Default.Equals(Value, other.Value);
            public override bool Equals(object obj) => obj is Key key && Equals(key);
            public override int GetHashCode() => EqualityComparer<object>.Default.GetHashCode(Value);
        }

        public readonly object Value;
        public readonly Type Type;
        public readonly int Index;
        public readonly Dictionary<Key, int> Indices;
        public readonly List<(IInstantiator instantiator, IInitializer initializer)> Pairs;

        public Context(in Context context)
        {
            Value = context.Value;
            Type = context.Type;
            Index = context.Index;
            Indices = context.Indices;
            Pairs = context.Pairs;
        }

        public Context(object value, Type type)
        {
            Value = value;
            Type = type;
            Index = 0;
            Indices = new Dictionary<Key, int>();
            Pairs = new List<(IInstantiator, IInitializer)>();
        }

        public Context(object value, Type type, in Context context) : this(context)
        {
            Value = value;
            Type = type;
        }

        public Context(int index, in Context context) : this(context)
        {
            Index = index;
        }

        public Reference Add(object value, IInstantiator instantiator, IInitializer initializer)
        {
            var key = new Key(value);
            var index = Pairs.Count;
            Indices[key] = index;
            Pairs.Add((instantiator, initializer));
            return new Reference(index, instantiator, initializer);
        }
    }
}
