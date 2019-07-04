using Entia.Modules;
using Entia.Queriers;
using Entia.Queryables;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Entia.Experiment
{
    public static class GroupTest
    {
        public static void Benchmark(int iterations)
        {
            var world = WorldTest.RandomWorld(iterations);
            var group = world.Groups().Get(world.Queriers().Get<All<Write<Position>, Read<Velocity>>>());
            var array = group.ToArray();
            var segments = world.Components().Segments;
            var arrays = segments
                .Where(segment => group.Querier.TryQuery(new Context(segment, world), out _))
                .Select(segment => (segment.Entities.count, positions: segment.Store<Position>(), velocities: segment.Store<Velocity>()))
                .ToArray();

            void Body(ref Position position, in Velocity velocity)
            {
                position.X += velocity.X;
                position.Y += velocity.Y;
                position.Z += velocity.Z;
            }

            void DirectFor()
            {
                for (var i = 0; i < arrays.Length; i++)
                {
                    var (count, positions, velocities) = arrays[i];
                    for (var j = 0; j < count; j++) Body(ref positions[j], velocities[j]);
                }
            }

            void DirectParallel1()
            {
                Parallel.For(0, arrays.Length, i =>
                {
                    var (count, positions, velocities) = arrays[i];
                    for (var j = 0; j < count; j++) Body(ref positions[j], velocities[j]);
                });
            }

            void DirectParallel2()
            {
                Parallel.For(0, arrays.Length, i =>
                {
                    var (count, positions, velocities) = arrays[i];
                    Parallel.For(0, count, j => Body(ref positions[j], velocities[j]));
                });
            }

            void SegmentFor()
            {
                for (var i = 0; i < segments.Length; i++)
                {
                    var segment = segments[i];
                    var count = segment.Entities.count;
                    if (segment.TryStore<Position>(out var positions) && segment.TryStore<Velocity>(out var velocities))
                        for (var j = 0; j < count; j++) Body(ref positions[j], velocities[j]);
                }
            }

            void ArrayFor()
            {
                for (var i = 0; i < array.Length; i++)
                {
                    ref var item = ref array[i];
                    Body(ref item.Value1.Value, item.Value2.Value);
                }
            }

            void ArrayForeach()
            {
                foreach (var item in array) Body(ref item.Value1.Value, item.Value2.Value);
            }

            void ArrayParallel()
            {
                Parallel.For(0, array.Length, index =>
                {
                    ref var item = ref array[index];
                    Body(ref item.Value1.Value, item.Value2.Value);
                });
            }

            void GroupForeach1()
            {
                foreach (var item in group) Body(ref item.Value1.Value, item.Value2.Value);
            }

            void GroupForeach2()
            {
                foreach (ref readonly var item in group) Body(ref item.Value1.Value, item.Value2.Value);
            }

            void GroupForeach3()
            {
                foreach (var segment in group.Segments)
                    foreach (ref readonly var item in segment) Body(ref item.Value1.Value, item.Value2.Value);
            }

            void GroupFor1()
            {
                for (var i = 0; i < group.Segments.Length; i++)
                {
                    ref readonly var segment = ref group.Segments[i];
                    for (var j = 0; j < segment.Count; j++)
                    {
                        ref readonly var item = ref segment.Items[j];
                        Body(ref item.Value1.Value, item.Value2.Value);
                    }
                }
            }

            void GroupFor2()
            {
                for (var i = 0; i < group.Segments.Length; i++)
                {
                    ref readonly var segment = ref group.Segments[i];
                    var count = segment.Count;
                    var positions = segment.Store<Position>();
                    var velocities = segment.Store<Velocity>();
                    for (var j = 0; j < count; j++) Body(ref positions[j], velocities[j]);
                }
            }

            void GroupParallel1()
            {
                Parallel.ForEach(group.Segments, segment =>
                {
                    for (var i = 0; i < segment.Count; i++)
                    {
                        ref readonly var item = ref segment.Items[i];
                        Body(ref item.Value1.Value, item.Value2.Value);
                    }
                });
            }

            void GroupParallel2()
            {
                Parallel.For(0, group.Segments.Length, i =>
                {
                    var segment = group.Segments[i];
                    Parallel.For(0, segment.Count, j =>
                    {
                        ref readonly var item = ref segment.Items[j];
                        Body(ref item.Value1.Value, item.Value2.Value);
                    });
                });
            }

            void GroupParallel3()
            {
                Parallel.For(0, group.Segments.Length, i =>
                {
                    ref readonly var segment = ref group.Segments[i];
                    var count = segment.Count;
                    var positions = segment.Store<Position>();
                    var velocities = segment.Store<Velocity>();
                    Parallel.For(0, count, j => Body(ref positions[j], velocities[j]));
                });
            }

            void GroupParallel4()
            {
                Parallel.For(0, group.Segments.Length, i =>
                {
                    ref readonly var segment = ref group.Segments[i];
                    var count = segment.Count;
                    var positions = segment.Store<Position>();
                    var velocities = segment.Store<Velocity>();
                    for (var j = 0; j < count; j++) Body(ref positions[j], velocities[j]);
                });
            }

            void GroupParallel5()
            {
                Parallel.ForEach(group.Split(Environment.ProcessorCount), split =>
                {
                    foreach (ref readonly var item in split) Body(ref item.Value1.Value, item.Value2.Value);
                });
            }

            void GroupTask1()
            {
                var tasks = group.Segments.Select(segment => Task.Run(() =>
                {
                    for (var i = 0; i < segment.Count; i++)
                    {
                        ref readonly var item = ref segment.Items[i];
                        Body(ref item.Value1.Value, item.Value2.Value);
                    }
                }));
                Task.WhenAll(tasks).Wait();
            }

            void GroupTask2()
            {
                var tasks = group.Split(Environment.ProcessorCount).Select(split => Task.Run(() =>
                {
                    foreach (ref readonly var item in split) Body(ref item.Value1.Value, item.Value2.Value);
                }));
                Task.WhenAll(tasks).Wait();
            }

            void GroupTask3()
            {
                var tasks = group.Segments.Select(segment => Task.Run(() =>
                {
                    var count = segment.Count;
                    var positions = segment.Store<Position>();
                    var velocities = segment.Store<Velocity>();
                    for (var j = 0; j < count; j++) Body(ref positions[j], velocities[j]);
                }));
                Task.WhenAll(tasks).Wait();
            }

            Console.WriteLine($"Size: {array.Length} | Processors: {Environment.ProcessorCount}");
            Test.Measure(
                DirectFor,
                new Action[]
                {
                    DirectParallel1,
                    DirectParallel2,
                    SegmentFor,
                    ArrayFor,
                    ArrayForeach,
                    ArrayParallel,
                    GroupForeach1,
                    GroupForeach2,
                    GroupForeach3,
                    GroupFor1,
                    GroupFor2,
                    GroupParallel1,
                    GroupParallel2,
                    GroupParallel3,
                    GroupParallel4,
                    GroupParallel5,
                    GroupTask1,
                    GroupTask2,
                    GroupTask3
                },
                1000,
                after: () => world.Resolve());
            Console.WriteLine();
        }

        public static void Run()
        {
            const int iterations = 100;
            var world = new World();
            var random = new Random();

            void SetRandom(Entity entity)
            {
                var value = random.NextDouble();
                if (value < 0.333)
                    world.Components().Set(entity, new Position { X = (float)value });
                else if (value < 0.666)
                    world.Components().Set(entity, new Velocity { X = (float)value });
            }

            for (var i = 0; i < iterations; i++)
            {
                var entity = world.Entities().Create();
                SetRandom(entity);
                SetRandom(entity);
            }

            world.Resolve();

            for (var i = 0; i < iterations; i++)
            {
                var entity = world.Entities().Create();
                SetRandom(entity);
                SetRandom(entity);
            }

            var group = world.Groups().Get(world.Queriers().Get<Write<Position>>());
            world.Entities().Clear();

            for (var i = 0; i < iterations; i++)
            {
                var entity = world.Entities().Create();
                SetRandom(entity);
                SetRandom(entity);
            }

            world.Resolve();

            foreach (var item in group)
            {
                item.Value.Y++;
                item.Value.Z--;
            }

            var items = group.ToArray();
            world.Resolve();

            for (var i = 1; i <= iterations; i++)
            {
                var entity = world.Entities().Create();

                var has1 = world.Components().Has<Position>(entity);
                var add1 = world.Components().Set(entity, new Position { X = i });
                ref var write1 = ref world.Components().Get<Position>(entity);
                write1.X++;

                var add2 = world.Components().Set(entity, new Position { Y = i });
                var has2 = world.Components().Has<Position>(entity);
                world.Resolve();
                var has3 = world.Components().Has<Position>(entity);
                var add3 = world.Components().Set(entity, new Position { Z = i });
                var add4 = world.Components().Set(entity, new Velocity { X = i });
                var get1 = world.Components().Get(entity).ToArray();
                world.Resolve();
                var get2 = world.Components().Get(entity).ToArray();

                ref var write2 = ref world.Components().Get<Position>(entity);
                write2.Y++;

                var has4 = world.Components().Has<Position>(entity);
                var has5 = world.Components().Has<Velocity>(entity);
                var remove1 = world.Components().Remove<Position>(entity);
                world.Resolve();
                var has6 = world.Components().Has<Velocity>(entity);
                ref var write3 = ref world.Components().Get<Velocity>(entity);
                write3.X++;
                var clear1 = world.Components().Clear(entity);
                var get3 = world.Components().Get(entity).ToArray();
                var add5 = world.Components().Set(entity, new Position { X = i + 10 });
                var has7 = world.Components().Has<Position>(entity);
                var has8 = world.Components().Has<Velocity>(entity);
                world.Resolve();

                var get4 = world.Components().Get(entity).ToArray();
                var has9 = world.Components().Has<Velocity>(entity);
                ref var write4 = ref world.Components().Get<Position>(entity);
                write4.Y++;
            }
        }
    }
}