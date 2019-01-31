using Entia.Initializers;
using Entia.Instantiators;
using System;
using System.Collections.Generic;

namespace Entia.Modules.Template
{
	public sealed class Context
	{
		public readonly struct Key : IEquatable<Key>
		{
			public readonly object Value;
			public Key(object value) { Value = value; }
			public bool Equals(Key other) => EqualityComparer<object>.Default.Equals(Value, other.Value);
			public override bool Equals(object obj) => obj is Key key && Equals(key);
			public override int GetHashCode() => EqualityComparer<object>.Default.GetHashCode(Value);
		}

		public readonly int Index;
		public readonly Dictionary<Key, int> Indices = new Dictionary<Key, int>();
		public readonly List<(IInitializer initializer, IInstantiator instantiator)> Pairs = new List<(IInitializer initializer, IInstantiator instantiator)>();
	}
}
