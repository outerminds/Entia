using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia;
using Entia.Core;
using Entia.Messages.Segment;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Modules.Group
{
    public interface IGroup
    {
        int Count { get; }
        Type Type { get; }
        IQuerier Querier { get; }
        IEnumerable<Entity> Entities { get; }

        bool Has(Entity entity);
    }

    public sealed class Group<T> : IGroup, IEnumerable<T> where T : struct, IQueryable
    {
        public struct Enumerator : IEnumerator<T>
        {
            public ref readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _items[_index];
            }

            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            Segment<T>[] _segments;
            T[] _items;
            int _segment;
            int _index;
            int _count;

            public Enumerator(Segment<T>[] segments)
            {
                _segments = segments;
                _items = Array.Empty<T>();
                _segment = -1;
                _index = -1;
                _count = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (++_index < _count) return true;
                while (++_segment < _segments.Length)
                {
                    var segment = _segments[_segment];
                    _count = segment.Count;
                    if (_count > 0)
                    {
                        _items = segment.Items;
                        _index = 0;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                _segment = -1;
                _index = -1;
            }

            public void Dispose()
            {
                _segments = default;
                _items = default;
            }
        }

        public readonly struct EntityEnumerable : IEnumerable<Entity>
        {
            readonly Segment<T>[] _segments;
            public EntityEnumerable(Segment<T>[] segments) { _segments = segments; }
            public EntityEnumerator GetEnumerator() => new EntityEnumerator(_segments);
            IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct EntityEnumerator : IEnumerator<Entity>
        {
            public Entity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _entities[_index];
            }

            object IEnumerator.Current => Current;

            Segment<T>[] _segments;
            Entity[] _entities;
            int _segment;
            int _index;
            int _count;

            public EntityEnumerator(Segment<T>[] segments)
            {
                _segments = segments;
                _entities = Array.Empty<Entity>();
                _segment = -1;
                _index = -1;
                _count = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (++_index < _count) return true;
                while (++_segment < _segments.Length)
                {
                    var segment = _segments[_segment];
                    _count = segment.Count;
                    if (_count > 0)
                    {
                        _entities = segment.Entities;
                        _index = 0;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                _segment = -1;
                _index = -1;
            }

            public void Dispose()
            {
                _segments = default;
                _entities = default;
            }
        }

        public readonly struct SplitEnumerable : IEnumerable<Split<T>>
        {
            readonly Segment<T>[] _segments;
            readonly int _size;

            public SplitEnumerable(Segment<T>[] segments, int size)
            {
                _segments = segments;
                _size = size;
            }

            public SplitEnumerator GetEnumerator() => new SplitEnumerator(_segments, _size);
            IEnumerator<Split<T>> IEnumerable<Split<T>>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct SplitEnumerator : IEnumerator<Split<T>>
        {
            public Split<T> Current => new Split<T>(_segments, _current.segment, _current.index, _count);
            object IEnumerator.Current => Current;

            Segment<T>[] _segments;
            int _size;
            int _count;
            (int segment, int index) _current;
            (int segment, int index) _next;

            public SplitEnumerator(Segment<T>[] segments, int size)
            {
                _segments = segments;
                _size = size;
                _count = 0;
                _current = (0, 0);
                _next = (0, 0);
            }

            public bool MoveNext()
            {
                _current = _next;
                _count = 0;

                while (_next.segment < _segments.Length)
                {
                    var segment = _segments[_next.segment];
                    var remaining = segment.Count - _next.index;
                    var minimum = Math.Min(_size - _count, remaining);
                    _count += minimum;

                    // NOTE: '_clamped' must never go over '_count'
                    if (_count == _size)
                    {
                        _next.index += minimum;
                        return _count > 0;
                    }

                    _next.segment++;
                    _next.index = 0;
                }

                return _count > 0;
            }

            public void Reset()
            {
                _current = (-1, -1);
                _next = (0, 0);
            }

            public void Dispose() { _segments = default; }
        }

        public int Count { get; private set; }
        public Segment<T>[] Segments => _segments;
        public EntityEnumerable Entities => new EntityEnumerable(_segments);

        IQuerier IGroup.Querier => _querier;
        Type IGroup.Type => typeof(T);
        IEnumerable<Entity> IGroup.Entities => Entities;

        readonly Querier<T> _querier;
        readonly World _world;
        readonly Components _components;
        readonly Messages _messages;
        Component.Segment[] _indexToComponentSegment = new Component.Segment[4];
        Query<T>[] _indexToQuery = new Query<T>[4];
        Segment<T>[] _segments = { };
        int[] _indexToSegment = new int[4];

        public Group(Querier<T> querier, World world)
        {
            _querier = querier;
            _world = world;
            _components = world.Components();
            _messages = world.Messages();
            _messages.React((in OnCreate message) => TryAdd(message.Segment));
            _messages.React((in OnMove message) => Move(message.Source, message.Target));
            foreach (var segment in _components.Segments) TryAdd(segment);
        }

        public bool Has(Entity entity) => _components.TryGetSegment(entity, out var pair) && Has(pair.segment);

        public bool TryGet(Entity entity, out T item)
        {
            if (_components.TryGetSegment(entity, out var pair) && Has(pair.segment))
            {
                item = _indexToQuery[pair.segment.Index].Get(pair.index);
                return true;
            }

            item = default;
            return false;
        }

        public SplitEnumerable Split(int count)
        {
            count = Math.Min(Count, count);
            var size = count == 0 ? 0 : Count / count;
            if (size > 0 && Count % size > 0) size++;
            return new SplitEnumerable(_segments, size);
        }

        public Enumerator GetEnumerator() => new Enumerator(_segments);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool Has(Component.Segment segment) => segment.Index < _indexToComponentSegment.Length && _indexToComponentSegment[segment.Index] == segment;

        bool TryAdd(Component.Segment segment)
        {
            if (!Has(segment) && _querier.TryQuery(segment, _world, out var query))
            {
                Count += segment.Entities.count;
                ArrayUtility.Set(ref _indexToComponentSegment, segment, segment.Index);
                ArrayUtility.Set(ref _indexToQuery, query, segment.Index);
                ArrayUtility.Set(ref _indexToSegment, _segments.Length, segment.Index);

                var items = new T[segment.Entities.items.Length];
                for (int i = 0; i < items.Length; i++) items[i] = query.Get(i);
                ArrayUtility.Add(ref _segments, new Segment<T>(segment, items));
                return true;
            }

            return false;
        }

        void Move(in (Component.Segment segment, int index) source, in (Component.Segment segment, int index) target)
        {
            var has = (source: Has(source.segment), target: Has(target.segment));
            Count += (has.source ? -1 : 0) + (has.target ? 1 : 0);

            if (has.source)
            {
                ref var segment = ref _segments[_indexToSegment[source.segment.Index]];
                var query = _indexToQuery[source.segment.Index];
                segment.Items[source.index] = query.Get(source.index);
            }

            if (has.target)
            {
                ref var segment = ref _segments[_indexToSegment[target.segment.Index]];
                var query = _indexToQuery[target.segment.Index];
                var items = segment.Items;
                var count = items.Length;
                if (ArrayUtility.Ensure(ref items, segment.Count))
                {
                    for (int i = count; i < segment.Count; i++) items[i] = query.Get(i);
                    segment = new Segment<T>(target.segment, items);
                }
                // NOTE: do this step after ensuring that the 'items' array is large enough
                items[target.index] = query.Get(target.index);
            }
        }
    }
}