using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia;
using Entia.Core;
using Entia.Messages.Segment;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queryables;

namespace Entia.Modules.Group
{
    public interface IGroup : IResolvable { }

    public sealed class Group<T> : IGroup, IEnumerable<(Entity entity, T item)> where T : struct, IQueryable
    {
        public struct Enumerator : IEnumerator<(Entity entity, T item)>
        {
            public (Entity entity, T item) Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (_entities.items[_index.Value], _value);
            }
            object IEnumerator.Current => Current;

            Group<T> _group;
            int _segment;
            Box<int> _index;
            T _value;
            (Entity[] items, int count) _entities;

            public Enumerator(Group<T> group, int segment = 0, int entity = 0)
            {
                _group = group;
                _segment = int.MaxValue - 1;
                _index = _group.Box(int.MaxValue - 1);
                _entities = (Array.Empty<Entity>(), 0);
                _value = default;
                // NOTE: use 'segment', not '_segment' and use 'entity', not '_entity' here
                Update(segment, entity - 1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index.Value < _entities.count || Update(_segment + 1, 0);

            bool Update(int segment, int entity)
            {
                for (int i = segment; i < _group._segments.count; i++)
                {
                    var current = _group._segments.items[i];
                    _entities = current.Entities;
                    if (entity >= _entities.count) { entity -= _entities.count; continue; }

                    _segment = i;
                    _index.Value = entity;
                    _value = _group._indexToQuery[current.Index].Get(_index);
                    return true;
                }

                return false;
            }

            public void Reset() => Update(0, 0);
            public void Dispose() => _group = null;
        }

        public readonly struct SplitEnumerable : IEnumerable<SegmentEnumerable>
        {
            readonly Group<T> _group;
            readonly int _count;

            public SplitEnumerable(Group<T> group, int count)
            {
                _group = group;
                _count = count;
            }

            public SplitEnumerator GetEnumerator() => new SplitEnumerator(_group, _count);
            IEnumerator<SegmentEnumerable> IEnumerable<SegmentEnumerable>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct SplitEnumerator : IEnumerator<SegmentEnumerable>
        {
            public SegmentEnumerable Current => new SegmentEnumerable(_group, _count, _current.segment, _current.entity);
            object IEnumerator.Current => Current;

            Group<T> _group;
            int _count;
            (int segment, int entity) _current;
            (int segment, int entity) _next;

            public SplitEnumerator(Group<T> group, int count)
            {
                _group = group;
                _count = count;
                _current = (0, 0);
                _next = (0, 0);
            }

            public bool MoveNext()
            {
                _current = _next;
                var current = _count;

                while (_next.segment < _group._segments.count)
                {
                    var segment = _group._segments.items[_next.segment];
                    var remaining = segment.Entities.count - _next.entity;
                    var minimum = Math.Min(current, remaining);
                    _next.entity += minimum;
                    current -= minimum;
                    if (current <= 0) return true;

                    _next.segment++;
                    _next.entity = 0;
                }

                return current < _count;
            }

            public void Reset()
            {
                _current = (0, 0);
                _next = (0, 0);
            }

            public void Dispose() => _group = null;
        }

        public readonly struct SegmentEnumerable : IEnumerable<(Entity, T)>
        {
            readonly Group<T> _group;
            readonly int _count;
            readonly int _segment;
            readonly int _entity;

            public SegmentEnumerable(Group<T> group, int count, int segment, int entity)
            {
                _group = group;
                _count = count;
                _segment = segment;
                _entity = entity;
            }

            public SegmentEnumerator GetEnumerator() => new SegmentEnumerator(_group, _count, _segment, _entity);
            IEnumerator<(Entity, T)> IEnumerable<(Entity, T)>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct SegmentEnumerator : IEnumerator<(Entity entity, T item)>
        {
            public (Entity entity, T item) Current => _enumerator.Current;
            object IEnumerator.Current => Current;

            Enumerator _enumerator;
            int _count;
            int _index;

            public SegmentEnumerator(Group<T> group, int count, int segment, int entity)
            {
                _enumerator = new Enumerator(group, segment, entity);
                _count = count;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _count && _enumerator.MoveNext();
            public void Reset()
            {
                _enumerator.Reset();
                _index = -1;
            }
            public void Dispose() => _enumerator.Dispose();
        }

        public int Count { get; private set; }

        readonly Components _components;
        readonly Queriers _queriers;
        readonly Messages _messages;
        readonly Pool<Box<int>> _pool = new Pool<Box<int>>(() => new Box<int>());
        (Box<int>[] items, int count) _boxes = (new Box<int>[2], 0);
        (Segment[] items, int count) _segments = (new Segment[4], 0);
        Segment[] _indexToSegment = new Segment[4];
        Query<T>[] _indexToQuery = new Query<T>[4];

        public Group(Components components, Queriers queriers, Messages messages)
        {
            _components = components;
            _queriers = queriers;
            _messages = messages;
            _messages.React((in OnCreate message) => TryAdd(message.Segment));
            _messages.React((in OnMove message) => Move(message.Source, message.Target));
            foreach (var segment in _components.Segments) TryAdd(segment);
        }

        public SplitEnumerable Split(int count) => new SplitEnumerable(this, count);

        public bool Has(Entity entity) => _components.TryGetSegment(entity, out var pair) && Has(pair.segment);

        public bool TryGet(Entity entity, out T item)
        {
            if (_components.TryGetSegment(entity, out var pair) && Has(pair.segment))
            {
                item = _indexToQuery[pair.segment.Index].Get(Box(pair.index));
                return true;
            }

            item = default;
            return false;
        }

        public void Resolve()
        {
            for (int i = 0; i < _boxes.count; i++) _pool.Put(_boxes.items[i]);
            _boxes.Clear();
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(Entity entity, T item)> IEnumerable<(Entity entity, T item)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        Box<int> Box(int value = 0)
        {
            Box<int> box;
            lock (_pool.Lock) { _boxes.Push(box = _pool.Take()); }
            box.Value = value;
            return box;
        }

        bool Has(Segment segment) => segment.Index < _indexToSegment.Length && _indexToSegment[segment.Index] == segment;

        bool TryAdd(Segment segment)
        {
            if (!Has(segment) && _queriers.TryQuery<T>(segment, out var query))
            {
                ArrayUtility.Set(ref _indexToSegment, segment, segment.Index);
                ArrayUtility.Set(ref _indexToQuery, query, segment.Index);
                _segments.Push(segment);
                Count += segment.Entities.count;
                return true;
            }

            return false;
        }

        void Move(in (Segment segment, int index) source, in (Segment segment, int index) target)
        {
            var has = (source: Has(source.segment), target: Has(target.segment));
            Count += (has.source ? -1 : 0) + (has.target ? 1 : 0);
        }
    }
}