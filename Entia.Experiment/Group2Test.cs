using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Entia.Core;
using Entia.Messages.Segment;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Group;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Experiment
{
    public static class Group2Test
    {
        public abstract class Itemizer<T> where T : struct, IQueryable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public abstract T Create(Array[] stores, int store, int index);
        }

        public static class Query
        {
            public readonly struct Write<T> : IQueryable where T : struct, IComponent
            {
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

            public readonly struct All<T1, T2> : IQueryable where T1 : struct, IQueryable where T2 : struct, IQueryable
            {
                public readonly T1 Value1;
                public readonly T2 Value2;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public All(in T1 value1, in T2 value2)
                {
                    Value1 = value1;
                    Value2 = value2;
                }
            }
        }

        public static class Itemizer
        {
            public sealed class Write<T> : Itemizer<Query.Write<T>> where T : struct, IComponent
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public override Query.Write<T> Create(Array[] stores, int store, int index) => new Query.Write<T>((T[])stores[store], index);
            }

            public sealed class All<T1, T2, TItem1, TItem2> : Itemizer<Query.All<T1, T2>>
                where T1 : struct, IQueryable where T2 : struct, IQueryable
                where TItem1 : Itemizer<T1> where TItem2 : Itemizer<T2>
            {
                readonly TItem1 _itemizer1;
                readonly TItem2 _itemizer2;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public override Query.All<T1, T2> Create(Array[] stores, int store, int index) =>
                    new Query.All<T1, T2>(_itemizer1.Create(stores, store, index), _itemizer2.Create(stores, store + 1, index));

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public All(TItem1 itemizer1, TItem2 itemizer2)
                {
                    _itemizer1 = itemizer1;
                    _itemizer2 = itemizer2;
                }
            }
        }

        public sealed class Group<T, TItem> where T : struct, IQueryable where TItem : Itemizer<T>
        {
            public struct Enumerator
            {
                public (Entity entity, T item) Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => (_current.Entities[_index], _current[_index]);
                }

                readonly Segmentz[] _segments;
                Segmentz _current;
                int _segment;
                int _index;

                public Enumerator(Segmentz[] segments)
                {
                    _segments = segments;
                    _current = default;
                    _segment = -1;
                    _index = -1;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    if (++_index < _current.Count) return true;
                    while (++_segment < _segments.Length)
                    {
                        _current = _segments[_segment];
                        if (_current.Count > 0)
                        {
                            _index = 0;
                            return true;
                        }
                    }
                    return false;
                }
            }

            public readonly struct Segmentz
            {
                public T this[int index]
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _itemizer.Create(Stores, 0, index);
                }

                public readonly int Count;
                public readonly Entity[] Entities;
                public readonly Array[] Stores;

                readonly TItem _itemizer;

                public Segmentz(int count, Entity[] entities, Array[] stores, TItem itemizer)
                {
                    Count = count;
                    Entities = entities;
                    Stores = stores;
                    _itemizer = itemizer;
                }
            }

            public Segmentz[] Segments => _segmentz;

            readonly IQuerier _querier;
            readonly TItem _itemizer;
            readonly World _world;
            Segmentz[] _segmentz = { };
            (Segment[] items, int count) _segments = (new Segment[4], 0);
            Segment[] _indexToSegment = new Segment[4];

            public Group(IQuerier querier, TItem itemizer, World world)
            {
                _querier = querier;
                _itemizer = itemizer;
                _world = world;
                world.Messages().React((in OnCreate message) => TryAdd(message.Segment));
                foreach (var segment in world.Components().Segments) TryAdd(segment);
            }

            public (Entity entity, T item)[] ToArray()
            {
                var list = new List<(Entity, T)>();
                foreach (var segment in _segmentz)
                    for (int i = 0; i < segment.Count; i++) list.Add((segment.Entities[i], segment[i]));
                return list.ToArray();
            }

            public Enumerator GetEnumerator() => new Enumerator(_segmentz);

            bool Has(Segment segment) => segment.Index < _indexToSegment.Length && _indexToSegment[segment.Index] == segment;

            bool TryAdd(Segment segment)
            {
                if (!Has(segment) && _querier.TryQuery(segment, _world, out var query))
                {
                    ArrayUtility.Set(ref _indexToSegment, segment, segment.Index);
                    _segments.Push(segment);

                    var index = _segmentz.Length;
                    Array.Resize(ref _segmentz, index + 1);

                    var stores = new Array[query.Types.Length];
                    for (int i = 0; i < query.Types.Length; i++) stores[i] = segment.GetStore(query.Types[i].Index);
                    _segmentz[index] = new Segmentz(segment.Entities.count, segment.Entities.items, stores, _itemizer);
                    return true;
                }

                return false;
            }
        }

        public static void Benchmark(int iterations)
        {
            var world = WorldTest.RandomWorld(iterations);
            var querier = world.Queriers().Get<All<Write<Position>, Write<Velocity>>>();
            var itemizer = new Itemizer.All<Query.Write<Position>, Query.Write<Velocity>, Itemizer.Write<Position>, Itemizer.Write<Velocity>>(
                new Itemizer.Write<Position>(),
                new Itemizer.Write<Velocity>());
            var group = new Group<
                Query.All<Query.Write<Position>, Query.Write<Velocity>>, Itemizer.All<Query.Write<Position>,
                Query.Write<Velocity>, Itemizer.Write<Position>, Itemizer.Write<Velocity>>>
                (querier, itemizer, world);
            var array = group.ToArray();

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
                    ref var item = ref array[index].item;
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                });
            }

            void GroupForeach()
            {
                foreach (var (_, item) in group)
                {
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void GroupParallel()
            {
                Parallel.ForEach(group.Segments, segment =>
                {
                    for (int i = 0; i < segment.Count; i++)
                    {
                        var item = segment[i];
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