using System;
using System.Collections;
using System.Collections.Generic;
using Entia.Core;
using Entia.Messages.Segment;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queryables;

namespace Entia.Messages.Segment
{
    public struct OnCreate : IMessage
    {
        public Modules.Component.Segment Segment;
    }

    public struct OnMove : IMessage
    {
        public (Modules.Component.Segment segment, int index) Source;
        public (Modules.Component.Segment segment, int index) Target;
    }
}

namespace Entia.Modules
{
    public sealed class Group3<T> : IEnumerable<T> where T : struct, IQueryable
    {
        public struct Enumerator : IEnumerator<T>
        {
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            Group3<T> _group;
            int _segment;
            int _entity;
            (Entity[] items, int count) _entities;

            public Enumerator(Group3<T> group, int segment = 0, int entity = 0)
            {
                _group = group;
                _segment = int.MaxValue;
                _entity = int.MaxValue;
                _entities = (Array.Empty<Entity>(), 0);
                Current = default;
                // NOTE: use 'segment', not '_segment' and use 'entity', not '_entity' here
                Update(segment, entity);
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (++_entity < _entities.count) return true;
                    else if (!Update(_segment + 1, 0)) return false;
                }
            }

            bool Update(int segment, int entity)
            {
                if (segment < _group._segments.count)
                {
                    var pair = _group._segments.items[segment];
                    _segment = segment;
                    _entity = entity - 1;
                    _entities = pair.segment.Entities;
                    // TODO: initialize item with index pointer
                    Current = pair.item;
                    return true;
                }

                return false;
            }

            public void Reset() => Update(0, 0);

            public void Dispose()
            {
                _group = null;
                _entities = default;
            }
        }

        public readonly struct SplitEnumerable : IEnumerable<SegmentEnumerable>
        {
            readonly Group3<T> _group;
            readonly int _count;

            public SplitEnumerable(Group3<T> group, int count)
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

            Group3<T> _group;
            int _count;
            (int segment, int entity) _current;
            (int segment, int entity) _next;

            public SplitEnumerator(Group3<T> group, int count)
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
                    var segment = _group._segments.items[_next.segment].segment;
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

        public readonly struct SegmentEnumerable : IEnumerable<T>
        {
            readonly Group3<T> _group;
            readonly int _count;
            readonly int _segment;
            readonly int _entity;

            public SegmentEnumerable(Group3<T> group, int count, int segment, int entity)
            {
                _group = group;
                _count = count;
                _segment = segment;
                _entity = entity;
            }

            public SegmentEnumerator GetEnumerator() => new SegmentEnumerator(_group, _count, _segment, _entity);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct SegmentEnumerator : IEnumerator<T>
        {
            public T Current => _enumerator.Current;
            object IEnumerator.Current => Current;

            Enumerator _enumerator;
            int _count;
            int _index;

            public SegmentEnumerator(Group3<T> group, int count, int segment, int entity)
            {
                _enumerator = new Enumerator(group, segment, entity);
                _count = count;
                _index = -1;
            }

            public bool MoveNext() => ++_index < _count && _enumerator.MoveNext();

            public void Reset()
            {
                _enumerator.Reset();
                _index = -1;
            }

            public void Dispose() => _enumerator.Dispose();
        }

        public int Count { get; private set; }
        public Query<T> Query;

        readonly Components3 _components;
        readonly Messages _messages;
        ((Segment segment, T item)[] items, int count) _segments = (new (Segment, T)[4], 0);
        Segment[] _indexToSegment = new Segment[4];

        public Group3(Components3 components, Messages messages)
        {
            _components = components;
            _messages = messages;
            _messages.React((in OnCreate message) => TryAdd(message.Segment));
            _messages.React((in OnMove message) => Move(message.Source, message.Target));
        }

        public SplitEnumerable Split(int count) => new SplitEnumerable(this, count);

        // public bool Has(Entity entity) => _components.TryGetSegment(entity, out var pair) && Has(pair.segment);
        public bool Has(Segment segment) => segment.Index < _indexToSegment.Length && _indexToSegment[segment.Index] == segment;

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool TryAdd(Segment segment)
        {
            if (!Has(segment) && Query.Fits(segment.Mask))
            {
                ArrayUtility.Set(ref _indexToSegment, segment, segment.Index);
                _indexToSegment[segment.Index] = segment;
                // TODO: build default item for segment
                _segments.Push((segment, default));
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