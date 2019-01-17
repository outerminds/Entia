using System;
using System.Linq;
using System.Threading.Tasks;
using Entia.Modules;
using Entia.Queryables;
using Entia.Modules.Group;

namespace Entia.Experiment
{
    public static class GroupTest
    {
        public static void Benchmark(int iterations)
        {
            var world = WorldTest.RandomWorld(iterations);
            var group = world.Groups().Get(world.Queriers().Get<All<Write<Position>, Read<Velocity>>>());
            var array = group.ToArray();
            var segments = world.Components().Segments
                .Where(segment => group.Querier.TryQuery(segment, world, out _))
                .ToArray();
            var arrays = segments
                .Select(segment => (segment.Entities.count, positions: segment.GetStore<Position>(), velocities: segment.GetStore<Velocity>()))
                .ToArray();

            void DirectFor()
            {
                for (int i = 0; i < arrays.Length; i++)
                {
                    var (count, positions, velocities) = arrays[i];
                    for (int j = 0; j < count; j++)
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    }
                }
            }

            void DirectParallel1()
            {
                Parallel.For(0, arrays.Length, i =>
                {
                    var (count, positions, velocities) = arrays[i];
                    for (int j = 0; j < count; j++)
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    }
                });
            }

            void DirectParallel2()
            {
                Parallel.For(0, arrays.Length, i =>
                {
                    var (count, positions, velocities) = arrays[i];
                    Parallel.For(0, count, j =>
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    });
                });
            }

            void SegmentFor()
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    var segment = segments[i];
                    var count = segment.Entities.count;
                    var positions = segment.GetStore<Position>();
                    var velocities = segment.GetStore<Velocity>();
                    for (int j = 0; j < count; j++)
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    }
                }
            }

            void ArrayFor()
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ref var item = ref array[i];
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void ArrayForeach()
            {
                foreach (var item in array)
                {
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void ArrayParallel()
            {
                Parallel.For(0, array.Length, index =>
                {
                    ref var item = ref array[index];
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                });
            }

            void GroupForeach1()
            {
                foreach (var item in group)
                {
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void GroupForeach2()
            {
                foreach (ref readonly var item in group)
                {
                    ref var position = ref item.Value1.Value;
                    ref readonly var velocity = ref item.Value2.Value;
                    position.X += velocity.X;
                }
            }

            void GroupForeach3()
            {
                foreach (var segment in group.Segments)
                {
                    foreach (var item in segment.Items)
                    {
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
                }
            }

            void GroupFor1()
            {
                for (int i = 0; i < group.Segments.Length; i++)
                {
                    ref readonly var segment = ref group.Segments[i];
                    for (int j = 0; j < segment.Count; j++)
                    {
                        ref readonly var item = ref segment.Items[j];
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
                }
            }

            void GroupFor2()
            {
                for (int i = 0; i < group.Segments.Length; i++)
                {
                    ref readonly var segment = ref group.Segments[i];
                    var count = segment.Count;
                    var positions = segment.Store<Position>();
                    var velocities = segment.Store<Velocity>();
                    for (int j = 0; j < count; j++)
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    }
                }
            }

            void GroupParallel1()
            {
                Parallel.ForEach(group.Segments, segment =>
                {
                    foreach (var item in segment.Items)
                    {
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
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
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
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
                    Parallel.For(0, count, j =>
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    });
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
                    for (int j = 0; j < count; j++)
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    }
                });
            }

            void GroupParallel5()
            {
                Parallel.ForEach(group.Split(Environment.ProcessorCount), split =>
                {
                    foreach (ref readonly var item in split)
                    {
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
                });
            }

            void GroupTask1()
            {
                var tasks = group.Segments.Select(segment => Task.Run(() =>
                {
                    foreach (var item in segment.Items)
                    {
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
                }));
                Task.WhenAll(tasks).Wait();
            }

            void GroupTask2()
            {
                var tasks = group.Split(Environment.ProcessorCount).Select(split => Task.Run(() =>
                {
                    foreach (ref readonly var item in split)
                    {
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
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
                    for (int j = 0; j < count; j++)
                    {
                        ref var position = ref positions[j];
                        ref readonly var velocity = ref velocities[j];
                        position.X += velocity.X;
                    }
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
                after: world.Resolve);
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