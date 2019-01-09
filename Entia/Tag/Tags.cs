using Entia.Core;
using Entia.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
	public sealed class Tags : IModule, IEnumerable<Type>
	{
		readonly Entities _entities;
		readonly Messages _messages;

		public Tags(Entities entities, Messages messages)
		{
			_entities = entities;
			_messages = messages;
			_messages.React((in OnPreDestroy message) => Clear(message.Entity));
		}

		public bool Has(Entity entity, Type type) => _entities.TryMask(entity, out var mask) && IndexUtility<ITag>.TryGetIndex(type, out var index) && mask.Has(index.global);

		public bool Has<T>(Entity entity) where T : struct, ITag => _entities.TryMask(entity, out var mask) && mask.Has(IndexUtility<ITag>.Cache<T>.Index.global);

		public IEnumerable<Type> Get(Entity entity) => _entities.TryMask(entity, out var mask) ?
			IndexUtility<ITag>.GetMaskTypes(mask).Select(pair => pair.type) :
			Enumerable.Empty<Type>();

		public bool Set<T>(Entity entity) where T : struct, ITag
		{
			var (global, local) = IndexUtility<ITag>.Cache<T>.Index;
			if (_entities.TryMask(entity, out var mask) && mask.Add(global))
			{
				_messages.OnAddTag<T>(entity);
				return true;
			}

			return false;
		}

		public bool Set(Entity entity, Type type) => IndexUtility<ITag>.TryGetIndex(type, out var index) && Set(entity, index, type);

		public bool Remove(Entity entity, Type type)
		{
			if (IndexUtility<ITag>.TryGetIndex(type, out var index) && Remove(entity, index.global))
			{
				_messages.OnRemoveTag(entity, type, index.local);
				return true;
			}

			return false;
		}

		public bool Remove<T>(Entity entity) where T : struct, ITag
		{
			var (global, local) = IndexUtility<ITag>.Cache<T>.Index;
			if (Remove(entity, global))
			{
				_messages.OnRemoveTag<T>(entity);
				return true;
			}

			return false;
		}

		public bool CopyTo(Entity source, Entity target, Tags tags)
		{
			if (_entities.TryMask(source, out var mask))
			{
				foreach (var (global, local, type) in IndexUtility<ITag>.GetMaskTypes(mask)) tags.Set(target, (global, local), type);
				return true;
			}

			return false;
		}

		public bool Clear<T>() where T : struct, ITag
		{
			var cleared = false;
			var (global, local) = IndexUtility<ITag>.Cache<T>.Index;
			foreach (var (entity, data) in _entities.Pairs)
			{
				if (data.Mask.Remove(global))
				{
					cleared = true;
					_messages.OnRemoveTag<T>(entity);
				}
			}

			return cleared;
		}

		public bool Clear(Type type) => IndexUtility<ITag>.TryGetIndex(type, out var index) && Clear(index, type);

		public bool Clear(Entity entity)
		{
			if (_entities.TryMask(entity, out var mask))
			{
				var cleared = false;
				foreach (var (global, local, type) in IndexUtility<ITag>.GetMaskTypes(mask))
				{
					if (mask.Remove(global))
					{
						cleared = true;
						_messages.OnRemoveTag(entity, type, local);
					}
				}
				return cleared;
			}

			return false;
		}

		public bool Clear()
		{
			var cleared = false;
			foreach (var (global, local, type) in IndexUtility<ITag>.Types) cleared |= Clear((global, local), type);
			return cleared;
		}

		bool Set(Entity entity, (int global, int local) index, Type type)
		{
			if (_entities.TryMask(entity, out var mask) && mask.Add(index.global))
			{
				_messages.OnAddTag(entity, type, index.local);
				return true;
			}
			return false;
		}

		bool Remove(Entity entity, int global) => _entities.TryMask(entity, out var mask) && mask.Remove(global);

		bool Clear((int global, int local) index, Type type)
		{
			var cleared = false;
			foreach (var (entity, data) in _entities.Pairs)
			{
				if (data.Mask.Remove(index.global))
				{
					cleared = true;
					_messages.OnRemoveTag(entity, type, index.local);
				}
			}

			return cleared;
		}

		public IEnumerator<Type> GetEnumerator() => _entities
			.SelectMany(entity => _entities.TryMask(entity, out var mask) ? IndexUtility<ITag>.GetMaskTypes(mask).Select(pair => pair.type) : Type.EmptyTypes)
			.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
