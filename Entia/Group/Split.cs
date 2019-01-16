using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia.Queryables;

namespace Entia.Modules.Group
{
    public readonly struct Split<T> : IEnumerable<(Entity entity, T item)> where T : struct, IQueryable
    {
        public struct Enumerator : IEnumerator<(Entity entity, T item)>
        {
            public (Entity entity, T item) Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (_entities[_index], _items[_index]);
            }
            object IEnumerator.Current => Current;

            Split<T> _split;
            int _segment;
            int _index;
            int _total;
            int _count;
            Entity[] _entities;
            T[] _items;

            public Enumerator(in Split<T> split)
            {
                _split = split;
                _segment = split._segment;
                _index = split._index - 1;
                _total = split.Count;

                var segment = split._segments[_segment];
                _entities = segment.Entities;
                _items = segment.Items;
                _count = segment.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // NOTE: check '_total' after the '_index' such that it is not decremented if the current segment is exhausted
                if (++_index < _count && --_total >= 0) return true;
                while (++_segment < _split._segments.Length)
                {
                    var segment = _split._segments[_segment];
                    _count = segment.Count;
                    if (_count > 0)
                    {
                        _entities = segment.Entities;
                        _items = segment.Items;
                        _index = 0;
                        return --_total >= 0;
                    }
                }

                return false;
            }

            public void Reset()
            {
                _segment = _split._segment;
                _index = _split._index;
                _total = _split.Count;

                var segment = _split._segments[_segment];
                _entities = segment.Entities;
                _items = segment.Items;
                _count = segment.Count;
            }

            public void Dispose()
            {
                _split = default;
                _entities = default;
                _items = default;
            }
        }

        public readonly struct EntityEnumerable : IEnumerable<Entity>
        {
            readonly Split<T> _split;
            public EntityEnumerable(in Split<T> split) { _split = split; }
            public EntityEnumerator GetEnumerator() => new EntityEnumerator(_split);
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

            Split<T> _split;
            int _segment;
            int _index;
            int _total;
            int _count;
            Entity[] _entities;

            public EntityEnumerator(in Split<T> split)
            {
                _split = split;
                _segment = split._segment;
                _index = split._index - 1;
                _total = split.Count;

                var segment = split._segments[_segment];
                _entities = segment.Entities;
                _count = segment.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // NOTE: check '_total' after the '_index' such that it is not decremented if the current segment is exhausted
                if (++_index < _count && _total-- > 0) return true;
                while (++_segment < _split._segments.Length)
                {
                    var segment = _split._segments[_segment];
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
                _segment = _split._segment;
                _index = _split._index;
                _total = _split.Count;

                var segment = _split._segments[_segment];
                _entities = segment.Entities;
                _count = segment.Count;
            }

            public void Dispose()
            {
                _split = default;
                _entities = default;
            }
        }

        public readonly struct ItemEnumerable : IEnumerable<T>
        {
            readonly Split<T> _split;
            public ItemEnumerable(in Split<T> split) { _split = split; }
            public ItemEnumerator GetEnumerator() => new ItemEnumerator(_split);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct ItemEnumerator : IEnumerator<T>
        {
            public ref readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _items[_index];
            }
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            Split<T> _split;
            int _segment;
            int _index;
            int _total;
            int _count;
            T[] _items;

            public ItemEnumerator(in Split<T> split)
            {
                _split = split;
                _segment = split._segment;
                _index = split._index - 1;
                _total = split.Count;

                var segment = split._segments[_segment];
                _items = segment.Items;
                _count = segment.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // NOTE: check '_total' after the '_index' such that it is not decremented if the current segment is exhausted
                if (++_index < _count && _total-- > 0) return true;
                while (++_segment < _split._segments.Length)
                {
                    var segment = _split._segments[_segment];
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
                _segment = _split._segment;
                _index = _split._index;
                _total = _split.Count;

                var segment = _split._segments[_segment];
                _items = segment.Items;
                _count = segment.Count;
            }

            public void Dispose()
            {
                _split = default;
                _items = default;
            }
        }

        public readonly int Count;
        public ItemEnumerable Items => new ItemEnumerable(this);
        public EntityEnumerable Entities => new EntityEnumerable(this);

        readonly Segment<T>[] _segments;
        readonly int _segment;
        readonly int _index;

        public Split(Segment<T>[] segments, int segment, int index, int count)
        {
            _segments = segments;
            _segment = segment;
            _index = index;
            Count = count;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<(Entity entity, T item)> IEnumerable<(Entity entity, T item)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}