using Entia.Core;
using Entia.Modules.Query;
using Entia.Queryables;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules.Group
{
    public interface IGroup
    {
        int Count { get; }
        IEnumerable<Entity> Entities { get; }

        IQuery Query { get; }
        Type Type { get; }

        bool Has(Entity entity);
        bool Update(Entity entity);
        bool Fits(Entity entity);
        bool Remove(Entity entity);
        bool Clear();
    }

    public sealed class Group<T> : IGroup, IEnumerable<T> where T : struct, IQueryable
    {
        public struct Enumerator : IEnumerator<T>
        {
            public ref readonly T Current => ref _enumerator.Current;
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            SwissList<T>.Enumerator _enumerator;

            public Enumerator(SwissList<T>.Enumerator enumerator) { _enumerator = enumerator; }

            public bool MoveNext() => _enumerator.MoveNext();
            public void Reset() => _enumerator.Reset();
            public void Dispose() => _enumerator.Dispose();
        }

        const int _sentinel = int.MaxValue;

        public int Count => _indices.Count;
        public Query<T> Query { get; }
        public IEnumerable<Entity> Entities => _indices.Keys;
        public ref readonly T this[int index] => ref _items[_indirectToDirect[index]];

        IQuery IGroup.Query => Query;
        Type IGroup.Type => typeof(T);

        readonly Entities _entities;
        readonly Dictionary<Entity, int> _indices = new Dictionary<Entity, int>();
        readonly SwissList<T> _items = new SwissList<T>();
        readonly SwitchList<int> _indirectToDirect = new SwitchList<int>();
        int[] _directToIndirect = new int[4];

        public Group(Query<T> query, Entities entities)
        {
            Query = query;
            _entities = entities;
        }

        public bool Has(Entity entity) => _indices.ContainsKey(entity);

        public bool Fits(Entity entity) => _entities.TryMask(entity, out var mask) && Query.Fits(mask);

        public bool TryGet(Entity entity, out T item)
        {
            if (_indices.TryGetValue(entity, out var index) && _items.TryGet(index, out item)) return true;
            item = default;
            return false;
        }

        public bool Update(Entity entity)
        {
            if (Create(entity, out var item))
            {
                Allocate(entity, item);
                return true;
            }
            else
            {
                Free(entity);
                return false;
            }
        }

        public bool Remove(Entity entity) => Free(entity);

        public bool Clear()
        {
            var cleared = _items.Clear() | _indirectToDirect.Clear() | _indices.Count > 0;
            _directToIndirect.Clear();
            _indices.Clear();
            return cleared;
        }

        public T[] ToArray() => _items.ToArray();

        public Enumerator GetEnumerator() => new Enumerator(_items.GetEnumerator());
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool Create(Entity entity, out T item)
        {
            if (Fits(entity) && Query.TryGet(entity, out item)) return true;
            item = default;
            return false;
        }

        void Allocate(Entity entity, in T item)
        {
            if (_indices.TryGetValue(entity, out var index)) _items.TrySet(index, item);
            else
            {
                var direct = _items.Add(item);
                var indirect = _indirectToDirect.Add(direct);

                ArrayUtility.Ensure(ref _directToIndirect, direct + 1);
                _directToIndirect[direct] = indirect;
                _indices[entity] = direct;
            }
        }

        bool Free(Entity entity)
        {
            if (_indices.TryGetValue(entity, out var index))
            {
                _indices.Remove(entity);
                _items.Remove(index);

                var indirect = _directToIndirect[index];
                _indirectToDirect.Remove(indirect);
                _directToIndirect[index] = _sentinel;
                if (_indirectToDirect.TryGet(indirect, out var direct)) _directToIndirect[direct] = indirect;

                return true;
            }

            return false;
        }
    }
}
