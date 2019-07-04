using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages.Segment;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Modules.Group
{
    /// <summary>
    /// Interface that all groups must implement.
    /// </summary>
    public interface IGroup
    {
        /// <summary>
        /// Gets the current entity count that are in the group.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        int Count { get; }
        /// <summary>
        /// Gets the item type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        Type Type { get; }
        /// <summary>
        /// Gets the querier.
        /// </summary>
        /// <value>
        /// The querier.
        /// </value>
        IQuerier Querier { get; }
        /// <summary>
        /// Gets the entities currently in the group.
        /// </summary>
        /// <value>
        /// The entities.
        /// </value>
        IEnumerable<Entity> Entities { get; }

        /// <summary>
        /// Determines whether the group has the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns <c>true</c> if the group has the <paramref name="entity"/>; otherwise, <c>false</c>.</returns>
        bool Has(Entity entity);
    }

    /// <summary>
    /// Queries and caches all entities that satisfy the query of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The query type.</typeparam>
    public sealed partial class Group<T> : IGroup, IEnumerable<Group<T>.Enumerator, T> where T : struct, IQueryable
    {
        /// <summary>
        /// An enumerator that enumerates over the group items.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
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

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="segments">The segments.</param>
            public Enumerator(Segment<T>[] segments)
            {
                _segments = segments;
                _items = Array.Empty<T>();
                _segment = -1;
                _index = -1;
                _count = 0;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
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

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset()
            {
                _segment = -1;
                _index = -1;
            }

            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose()
            {
                _segments = default;
                _items = default;
            }
        }

        /// <summary>
        /// An enumerable that enumerates over the group entities.
        /// </summary>
        [ThreadSafe]
        public sealed class EntityEnumerable : IEnumerable<EntityEnumerator, Entity>
        {
            readonly Group<T> _group;

            /// <summary>
            /// Initializes a new instance of the <see cref="EntityEnumerable"/> struct.
            /// </summary>
            /// <param name="group">The group.</param>
            public EntityEnumerable(Group<T> group) { _group = group; }
            /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
            public EntityEnumerator GetEnumerator() => new EntityEnumerator(_group._segments);
            IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// An enumerator that enumerates over the group entities.
        /// </summary>
        public struct EntityEnumerator : IEnumerator<Entity>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
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

            /// <summary>
            /// Initializes a new instance of the <see cref="EntityEnumerator"/> struct.
            /// </summary>
            /// <param name="segments">The segments.</param>
            public EntityEnumerator(Segment<T>[] segments)
            {
                _segments = segments;
                _entities = Array.Empty<Entity>();
                _segment = -1;
                _index = -1;
                _count = 0;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
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

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset()
            {
                _segment = -1;
                _index = -1;
            }

            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose()
            {
                _segments = default;
                _entities = default;
            }
        }

        /// <summary>
        /// An enumerable that enumerates over splits of a given size.
        /// </summary>
        [ThreadSafe]
        public readonly struct SplitEnumerable : IEnumerable<SplitEnumerator, Split<T>>
        {
            readonly Segment<T>[] _segments;
            readonly int _size;

            /// <summary>
            /// Initializes a new instance of the <see cref="SplitEnumerable"/> struct.
            /// </summary>
            /// <param name="segments">The segments.</param>
            /// <param name="size">The size of the splits.</param>
            public SplitEnumerable(Segment<T>[] segments, int size)
            {
                _segments = segments;
                _size = size;
            }

            /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
            public SplitEnumerator GetEnumerator() => new SplitEnumerator(_segments, _size);
            IEnumerator<Split<T>> IEnumerable<Split<T>>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// An enumerator that enumerates over splits of a given size.
        /// </summary>
        public struct SplitEnumerator : IEnumerator<Split<T>>
        {
            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public Split<T> Current => new Split<T>(_segments, _current.segment, _current.index, _count);
            object IEnumerator.Current => Current;

            Segment<T>[] _segments;
            int _size;
            int _count;
            (int segment, int index) _current;
            (int segment, int index) _next;

            /// <summary>
            /// Initializes a new instance of the <see cref="SplitEnumerator"/> struct.
            /// </summary>
            /// <param name="segments">The segments.</param>
            /// <param name="size">The size of the splits.</param>
            public SplitEnumerator(Segment<T>[] segments, int size)
            {
                _segments = segments;
                _size = size;
                _count = 0;
                _current = (0, 0);
                _next = (0, 0);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
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

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset()
            {
                _current = (-1, -1);
                _next = (0, 0);
            }

            /// <inheritdoc cref="IDisposable.Dispose"/>
            public void Dispose() => _segments = default;
        }

        /// <inheritdoc cref="IGroup.Count"/>
        [ThreadSafe]
        public int Count { get; private set; }
        /// <summary>
        /// Gets the segments that fit the group query.
        /// </summary>
        /// <value>
        /// The segments.
        /// </value>
        [ThreadSafe]
        public Segment<T>[] Segments => _segments;
        /// <inheritdoc cref="IGroup.Entities"/>
        public readonly EntityEnumerable Entities;
        /// <inheritdoc cref="IGroup.Querier"/>
        public readonly Querier<T> Querier;

        IQuerier IGroup.Querier => Querier;
        Type IGroup.Type => typeof(T);
        IEnumerable<Entity> IGroup.Entities => Entities;

        readonly World _world;
        readonly Components _components;
        readonly Messages _messages;
        Component.Segment[] _indexToComponentSegment = new Component.Segment[4];
        Query<T>[] _indexToQuery = new Query<T>[4];
        Segment<T>[] _segments = Array.Empty<Segment<T>>();
        int[] _indexToSegment = new int[4];

        /// <summary>
        /// Initializes a new instance of the <see cref="Group{T}"/> class.
        /// </summary>
        /// <param name="querier">The querier.</param>
        /// <param name="world">The world.</param>
        public Group(Querier<T> querier, World world)
        {
            Querier = querier;
            Entities = new EntityEnumerable(this);
            _world = world;
            _components = world.Components();
            _messages = world.Messages();
            _messages.React((in OnCreate message) => TryAdd(message.Segment));
            _messages.React((in OnMove message) => Move(message.Source, message.Target));
            foreach (var segment in _components.Segments) TryAdd(segment);
        }

        /// <inheritdoc cref="IGroup.Has(Entity)"/>
        [ThreadSafe]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity entity) => _components.TrySegment(entity, out var pair) && Has(pair.segment);

        /// <summary>
        /// Tries to get the <paramref name="item"/> associated with the provided <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="item">The item.</param>
        /// <returns>Returns <c>true</c> if an <paramref name="item"/> was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(Entity entity, out T item)
        {
            item = Get(entity, out var success);
            return success;
        }

        [ThreadSafe]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Get(Entity entity, out bool success)
        {
            if (_components.TrySegment(entity, out var pair) && Has(pair.segment))
            {
                success = true;
                return ref _segments[_indexToSegment[pair.segment.Index]].Items[pair.index];
            }

            success = false;
            return ref Dummy<T>.Value;
        }

        /// <summary>
        /// Splits the group into splits of the same size.
        /// </summary>
        /// <param name="count">The amount of splits.</param>
        /// <returns>The split enumerable.</returns>
        [ThreadSafe]
        public SplitEnumerable Split(int count)
        {
            count = Math.Min(Count, count);
            var size = count == 0 ? 0 : Count / count;
            if (size > 0 && Count % size > 0) size++;
            return new SplitEnumerable(_segments, size);
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        [ThreadSafe]
        public Enumerator GetEnumerator() => new Enumerator(_segments);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [ThreadSafe]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Has(Component.Segment segment) => segment.Index < _indexToComponentSegment.Length && _indexToComponentSegment[segment.Index] == segment;

        bool TryAdd(Component.Segment segment)
        {
            if (!Has(segment) && Querier.TryQuery(new Context(segment, _world), out var query))
            {
                Count += segment.Entities.count;
                ArrayUtility.Set(ref _indexToComponentSegment, segment, (int)segment.Index);
                ArrayUtility.Set(ref _indexToQuery, query, (int)segment.Index);
                ArrayUtility.Set(ref _indexToSegment, _segments.Length, (int)segment.Index);

                // NOTE: less maintenance is required for the special case 'Group<Entity>'
                if (typeof(T) == typeof(Entity))
                    ArrayUtility.Add(ref _segments, new Segment<T>(segment, segment.Entities.items as T[]));
                else
                {
                    var items = new T[segment.Entities.items.Length];
                    for (var i = 0; i < items.Length; i++) items[i] = query.Get(i);
                    ArrayUtility.Add(ref _segments, new Segment<T>(segment, items));
                }
                return true;
            }

            return false;
        }

        void Move(in (Component.Segment segment, int index) source, in (Component.Segment segment, int index) target)
        {
            var has = (source: Has(source.segment), target: Has(target.segment));
            Count += (has.source ? -1 : 0) + (has.target ? 1 : 0);

            // NOTE: less maintenance is required for the special case 'Group<Entity>'
            if (typeof(T) == typeof(Entity))
            {
                if (has.target)
                {
                    ref var segment = ref _segments[_indexToSegment[target.segment.Index]];
                    if (segment.Items.Length != segment.Entities.Length) segment = new Segment<T>(target.segment, target.segment.Entities.items as T[]);
                }
            }
            else
            {
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
                        // NOTE: update all items since existing items are pointing to an old stores
                        for (var i = 0; i < segment.Count; i++) items[i] = query.Get(i);
                        segment = new Segment<T>(target.segment, items);
                    }
                    // NOTE: do this step after ensuring that the 'items' array is large enough
                    items[target.index] = query.Get(target.index);
                }
            }
        }
    }
}