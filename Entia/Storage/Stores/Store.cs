using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Stores
{
	public interface IStorable<T> where T : Factories.IFactory, new() { }

	public interface IStore
	{
		int Count { get; }
		int Capacity { get; }

		Type Type { get; }
		IEnumerable<(Entity entity, object value)> Pairs { get; }
		IEnumerable<Entity> Entities { get; }
		IEnumerable<object> Values { get; }
		object this[int index] { get; set; }

		bool TryIndex(Entity entity, out int index);
		bool Remove(Entity entity);
		bool Has(Entity entity);
		bool Add(Entity entity, object value);
		bool Set(Entity entity, object value);
		bool CopyTo(Entity source, Entity target, IStore store);
		bool Clear();
		bool Resolve();
	}

	public abstract class Store<T> : IStore where T : struct, IComponent
	{
		public abstract ref T this[int index] { get; }
		object IStore.this[int index] { get => this[index]; set => this[index] = (T)value; }

		public abstract int Count { get; }
		public abstract int Capacity { get; }
		public abstract IEnumerable<(Entity entity, T value)> Pairs { get; }
		public abstract IEnumerable<Entity> Entities { get; }
		public abstract IEnumerable<T> Values { get; }

		Type IStore.Type => typeof(T);
		IEnumerable<(Entity entity, object value)> IStore.Pairs => Pairs.Select(pair => (pair.entity, (object)pair.value));
		IEnumerable<object> IStore.Values => Values.Cast<object>();

		public abstract bool TryIndex(Entity entity, out int index);
		public abstract bool TryGet(Entity entity, out T[] buffer, out int index);
		public abstract bool Set(Entity entity, T value);
		public abstract bool Has(Entity entity);
		public abstract bool Add(Entity entity, T value);
		public abstract bool Remove(Entity entity);
		public abstract bool CopyTo(Entity source, Entity target, Store<T> store);
		public abstract bool Clear();
		public abstract bool Resolve();

		bool IStore.Set(Entity entity, object value) => value is T casted && Set(entity, casted);
		bool IStore.Add(Entity entity, object value) => value is T casted && Set(entity, casted);
		bool IStore.CopyTo(Entity source, Entity target, IStore store) => store is Store<T> casted && CopyTo(source, target, casted);
	}
}
