using Entia.Core;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Stores
{
	public sealed class Default<T> : Store<T> where T : struct, IComponent
	{
		public override int Capacity => _values.Capacity;
		public override int Count => _values.Count;
		public override ref T this[int index] => ref _values[index];
		public override IEnumerable<(Entity entity, T value)> Pairs => _indices.Select(pair => (pair.Key, _values[pair.Value]));
		public override IEnumerable<Entity> Entities => _indices.Keys;
		public override IEnumerable<T> Values => _values;

		readonly Buffer<T> _values = new Buffer<T>();
		readonly Dictionary<Entity, int> _indices = new Dictionary<Entity, int>();
		readonly Stack<int> _free = new Stack<int>();
		readonly Stack<int> _frozen = new Stack<int>();

		public override bool TryIndex(Entity entity, out int index) => _indices.TryGetValue(entity, out index);

		public override bool TryGet(Entity entity, out T[] buffer, out int index)
		{
			if (TryIndex(entity, out var current) && _values.TryGet(current, out buffer, out index)) return true;

			buffer = default;
			index = default;
			return false;
		}

		public override bool Add(Entity entity, T value)
		{
			var index = ReserveIndex();
			_values.Set(value, index);
			_indices[entity] = index;
			return true;
		}

		public override bool Set(Entity entity, T value)
		{
			if (_indices.TryGetValue(entity, out var index))
			{
				_values.Set(value, index);
				return false;
			}
			else
			{
				Add(entity, value);
				return true;
			}
		}

		public override bool Remove(Entity entity)
		{
			if (TryIndex(entity, out var index))
			{
				_indices.Remove(entity);
				_values.Remove(index);
				_frozen.Push(index);
				return true;
			}

			return false;
		}

		public override bool Has(Entity entity) => _indices.ContainsKey(entity);

		public override bool Clear()
		{
			var cleared = _values.Clear() | _indices.Count > 0 | _free.Count > 0 | _frozen.Count > 0;
			_indices.Clear();
			_free.Clear();
			_frozen.Clear();
			return cleared;
		}

		public override bool Resolve()
		{
			while (_frozen.Count > 0) _free.Push(_frozen.Pop());
			return _values.Compact();
		}

		public override bool CopyTo(Entity source, Entity target, Store<T> store)
		{
			if (TryGet(source, out var buffer, out var index))
			{
				store.Set(target, buffer[index]);
				return true;
			}

			return false;
		}

		int ReserveIndex() => _free.Count > 0 ? _free.Pop() : _values.Maximum;
	}
}