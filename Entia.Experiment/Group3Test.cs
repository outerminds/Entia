using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Entia.Core;
using Entia.Messages.Segment;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Group;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Experiment
{
    public static class Group3Test
    {
        public static class Query
        {
            public readonly struct Write<T> : IQueryable where T : struct, IComponent
            {
                sealed class Querier : Querier<Write<T>>
                {
                    public override bool TryQuery(Segment segment, World world, out Query<Write<T>> query)
                    {
                        if (segment.Has<T>())
                        {
                            var metadata = ComponentUtility.Cache<T>.Data;
                            query = new Query<Write<T>>(index => new Write<T>(segment.GetStore<T>(), index), metadata);
                            return true;
                        }

                        query = default;
                        return false;
                    }
                }

                [Querier]
                static readonly Querier _querier = new Querier();

                public ref T Value
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref _store[_index];
                }

                readonly T[] _store;
                readonly int _index;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Write(T[] store, int index)
                {
                    _store = store;
                    _index = index;
                }
            }
        }

        public sealed class Group<T> : IEnumerable<(Entity entity, T item)> where T : struct, IQueryable
        {
            public struct Enumerator : IEnumerator<(Entity entity, T item)>
            {
                /// <inheritdoc cref="IEnumerator{T}.Current"/>
                public ref readonly (Entity entity, T item) Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref _current.items[_index];
                }

                (Entity entity, T item) IEnumerator<(Entity entity, T item)>.Current => Current;
                object IEnumerator.Current => Current;

                ((Entity, T)[] items, int count)[] _segments;
                ((Entity, T)[] items, int count) _current;
                int _segment;
                int _index;

                public Enumerator(((Entity, T)[] items, int count)[] segments)
                {
                    _segments = segments;
                    _current = default;
                    _segment = -1;
                    _index = -1;
                }

                /// <inheritdoc cref="IEnumerator.MoveNext"/>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    if (++_index < _current.count) return true;
                    while (++_segment < _segments.Length)
                    {
                        _current = _segments[_segment];
                        if (_current.count > 0)
                        {
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
                    _current = default;
                }
            }

            public ((Entity entity, T item)[] items, int count)[] Segments = { };

            readonly Querier<T> _querier;
            readonly World _world;
            (Segment[] items, int count) _segments = (new Segment[4], 0);
            Segment[] _indexToSegment = new Segment[4];

            public Group(Querier<T> querier, World world)
            {
                _querier = querier;
                _world = world;
                world.Messages().React((in OnCreate message) => TryAdd(message.Segment));
                foreach (var segment in world.Components().Segments) TryAdd(segment);
            }

            /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
            public Enumerator GetEnumerator() => new Enumerator(Segments);
            IEnumerator<(Entity entity, T item)> IEnumerable<(Entity entity, T item)>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            bool Has(Segment segment) => segment.Index < _indexToSegment.Length && _indexToSegment[segment.Index] == segment;

            bool TryAdd(Segment segment)
            {
                if (!Has(segment) && _querier.TryQuery(segment, _world, out var query))
                {
                    ArrayUtility.Set(ref _indexToSegment, segment, segment.Index);
                    _segments.Push(segment);

                    var index = Segments.Length;
                    Array.Resize(ref Segments, index + 1);
                    var items = (new (Entity, T)[segment.Entities.count], 0);
                    for (int i = 0; i < segment.Entities.count; i++)
                        items.Push((segment.Entities.items[i], query.Get(i)));
                    Segments[index] = items;
                    return true;
                }

                return false;
            }
        }

        public static void Benchmark(int iterations)
        {
            var world = WorldTest.RandomWorld(iterations);
            var querier = world.Queriers().Get<All<Query.Write<Position>, Query.Write<Velocity>>>();
            var group = new Group<All<Query.Write<Position>, Query.Write<Velocity>>>(
                world.Queriers().Get<All<Query.Write<Position>, Query.Write<Velocity>>>(), world);
            var array = System.Linq.Enumerable.ToArray(group);

            void ArrayIndexer()
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ref var item = ref array[i].item;
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void ArrayForeach()
            {
                foreach (var (_, item) in array)
                {
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void ArrayParallel()
            {
                System.Threading.Tasks.Parallel.For(0, array.Length, index =>
                {
                    ref var pair = ref array[index];
                    ref var position = ref pair.item.Value1.Value;
                    ref readonly var velocity = ref pair.item.Value2.Value;
                    position.X += velocity.X;
                });
            }

            void GroupForeach()
            {
                foreach (ref readonly var pair in group)
                {
                    ref var position = ref pair.item.Value1.Value;
                    ref readonly var velocity = ref pair.item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void GroupParallel()
            {
                Parallel.ForEach(group.Segments, segment =>
                {
                    for (int i = 0; i < segment.count; i++)
                    {
                        ref readonly var item = ref segment.items[i].item;
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
                });
            }

            Action[] tests =
            {
                () => Test.Measure($"Array Foreach ({array.Length})", ArrayForeach, 1000) ,
                () => Test.Measure($"Array Indexer ({array.Length})", ArrayIndexer, 1000) ,
                () => Test.Measure($"Array Parallel ({array.Length})", ArrayParallel, 1000) ,
                () => Test.Measure($"Group Foreach ({array.Length})", GroupForeach, 1000),
                () => Test.Measure($"Group Parallel ({array.Length})", GroupParallel, 1000)
            };
            tests.Shuffle();
            foreach (var test in tests) { test(); world.Resolve(); }
            Console.WriteLine();
        }
    }
}