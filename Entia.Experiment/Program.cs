using Entia.Core;
using Entia.Injectables;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Nodes;
using Entia.Queryables;
using Entia.Systems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Entia.Nodes.Node;

namespace Entia.Experiment
{
    public struct Position : IComponent { public float X, Y, Z; }
    public struct Velocity : IComponent { public float X, Y, Z; }
    public struct Lifetime : IComponent { public float Remaining; }
    public struct Targetable : IComponent { }
    public struct Targeter : IComponent { public Entity? Current; public float Distance; }
    public struct Mass : IComponent { public float Value; }
    public struct Impure : IComponent { public Dictionary<int, List<DateTime>> Dates; }
    public struct IsDead : IComponent { }
    public struct IsInvincible : IComponent { }
    public struct Time : IResource { public float Current; public float Delta; }
    public struct Seed : IResource { public float Value; }
    public struct OnMove : IMessage { public Entity Entity; }

    public static class Program
    {
        static class Default<T>
        {
            static readonly T[] _array = new T[1];

            public static ref T Value => ref _array[0];
        }

        struct Jango : IRun, IInitialize, IDispose
        {
            double _value;

            public Jango(int value) { _value = value; }
            public void Initialize() => Console.WriteLine(nameof(Initialize));
            public void Run()
            {
                for (var i = 0; i < 1_000_000; i++)
                    _value += Math.Sqrt(i);
            }
            public void Dispose() => Console.WriteLine(nameof(Dispose));
        }

        public static class Systems
        {
            public struct A : IRun
            {
                public Components<Position> P;
                public void Run() => throw new NotImplementedException();
            }
            public struct B : IRun
            {
                public Emitter<OnMove> M;
                public Components<Position>.Read P;
                public void Run() => throw new NotImplementedException();
            }
            public struct C : IRun
            {
                public Components<Position>.Read P;
                public Components<Velocity> V;
                public void Run() => throw new NotImplementedException();
            }
            public struct D : IRun
            {
                public Components<Position>.Read P;
                public Components<Velocity>.Read V;
                public void Run() => throw new NotImplementedException();
            }
            public struct E : IRun
            {
                public Components<Lifetime> L;
                public Components<Targetable>.Read T;
                public void Run() => throw new NotImplementedException();
            }
            public struct F : IRun
            {
                public Components<Lifetime> L;
                public Components<Targetable> T;
                public void Run() => throw new NotImplementedException();
            }
            public struct G : IRun
            {
                public Emitter<OnMove> M;
                public void Run() => throw new NotImplementedException();
            }
            public struct H : IRun
            {
                public Emitter<OnMove> M;
                public void Run() => throw new NotImplementedException();
            }
            public struct I : IRun
            {
                public Components<Position>.Read P;
                public Components<Lifetime>.Read L;
                public void Run() => throw new NotImplementedException();
            }
            public struct J : IRun
            {
                public Reaction<OnMove> M;
                public Components<Position>.Read P;
                public void Run() => throw new NotImplementedException();
            }
            public struct K : IRun
            {
                public Components<Targetable>.Read T;
                public void Run() => throw new NotImplementedException();
            }
        }

        public class Boba
        {
            public int Simon;
            public List<int> Karl;
        }

        public struct Game : IResource
        {
            public bool Quit;
        }

        public struct DoQuit : IMessage { }

        static void Simple1()
        {
            var world = new World();
            var node = Node.Sequence(
                System<Systems.A>(),
                System<Systems.B>(),
                System<Systems.C>()
            );
            var game = world.Resources().Box<Game>();
            var result = world.Controllers().Run(node, () => !game.Value.Quit);
        }

        static void Simple2()
        {
            var world = new World();
            var node = Node.Sequence(
                System<Systems.A>(),
                System<Systems.B>(),
                System<Systems.C>()
            );

            if (world.Controllers().Control(node).TryValue(out var controller))
            {
                ref var game = ref world.Resources().Get<Game>();
                controller.Initialize();
                while (!game.Quit) controller.Run();
                controller.Dispose();
            }
        }

        static void Shuffle<T>(this T[] array)
        {
            var random = new Random();
            for (int i = 0; i < array.Length; i++)
            {
                var index = random.Next(array.Length);
                var item = array[i];
                array[i] = array[index];
                array[index] = item;
            }
        }

        static void Benchmark(int iterations)
        {
            var world = new World();
            var entities = world.Entities();
            var components = world.Components();
            var random = new Random();
            var list = new List<Entity>();

            Entity RandomEntity() => list.Count == 0 ? Entity.Zero : list[random.Next(list.Count)];
            void SetComponent<T>(in T component) where T : struct, IComponent => components.Set(RandomEntity(), component);
            void RemoveComponent<T>() where T : struct, IComponent => components.Remove<T>(RandomEntity());

            for (var i = 0; i < iterations; i++)
            {
                var value1 = random.NextDouble();
                if (value1 < 0.3) list.Add(entities.Create());
                else if (value1 < 0.4)
                {
                    var entity = RandomEntity();
                    if (entities.Destroy(entity)) list.Remove(entity);
                }
                else if (value1 < 0.8)
                {
                    for (var j = 0; j < 5; j++)
                    {
                        var value2 = random.NextDouble();
                        if (value2 < 0.25) SetComponent(new Position { X = (float)value1, Y = (float)value2 });
                        else if (value2 < 0.5) SetComponent(new Velocity { X = (float)value1, Y = (float)value2 });
                        else if (value2 < 0.75) SetComponent(new Lifetime { Remaining = (float)value1 + (float)value2 });
                        else SetComponent(new Mass { Value = (float)value1 + (float)value2 });
                    }
                }
                else
                {
                    var value2 = random.NextDouble();
                    if (value2 < 0.25) RemoveComponent<Position>();
                    else if (value2 < 0.5) RemoveComponent<Velocity>();
                    else if (value2 < 0.75) RemoveComponent<Lifetime>();
                    else RemoveComponent<Mass>();
                }
                world.Resolve();
            }

            var group = world.Groups().Get(world.Queriers().Get<All<Write<Position>, Read<Velocity>>>());
            var array = group.Select(pair => (pair.entity, position: pair.item.Value1.Value, velocity: pair.item.Value2.Value)).ToArray();
            group.ToArray();

            void ArrayIndexer()
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ref var item = ref array[i];
                    ref var position = ref item.position;
                    ref readonly var Velocity = ref item.velocity;
                    position.X += Velocity.X;
                }
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

            void GroupTask()
            {
                Task.WhenAll(group.Split(group.Count / Environment.ProcessorCount).Select(split => Task.Run(() =>
                {
                    foreach (var (_, item) in split)
                    {
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
                }))).Wait();
            }

            void GroupParallel()
            {
                System.Threading.Tasks.Parallel.ForEach(group.Split(group.Count / Environment.ProcessorCount), split =>
                {
                    foreach (var (_, item) in split)
                    {
                        ref var position = ref item.Value1.Value;
                        ref readonly var velocity = ref item.Value2.Value;
                        position.X += velocity.X;
                    }
                });
            }

            Action[] tests =
            {
                () => Measure($"Array Indexer ({group.Count})", ArrayIndexer, 1000),
                () => Measure($"Group Foreach ({group.Count})", GroupForeach, 1000),
                () => Measure($"Group Task.WhenAll ({group.Count})", GroupTask, 1000),
                () => Measure($"Group Parallel.ForEach ({group.Count})", GroupParallel, 1000)
            };
            tests.Shuffle();
            foreach (var test in tests) { test(); world.Resolve(); }
            Console.WriteLine();
        }

        static void Poulah()
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

            foreach (var (_, item) in group)
            {
                item.Value.Y++;
                item.Value.Z--;
            }

            var items = group.ToArray();
            var split = group.Split(10);
            var segments = split.Select(segment => segment.ToArray()).ToArray();
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

        delegate void Original<T1, T2>(ref T1 value, in T2 state);
        unsafe delegate void Casted(void* value, void* state);

        [StructLayout(LayoutKind.Sequential)]
        struct Datalou<T, TState>
        {
            public Original<T, TState> Resolve;
            public TState State;
        }

        struct Datalou
        {
            public Casted Resolve;
            public Unit State;
        }

        unsafe static void VeryUnsafe()
        {
            var original = new Original<int, int>((ref int a, in int b) => a += b);
            var casted = Unsafe.As<Casted>(original);
            var value = 123;
            var pointer = Unsafe.AsPointer(ref value);
            casted(pointer, pointer);
        }

        unsafe static void ExtremeUnsafe()
        {
            var datalou = new Datalou<int, (string, string)>
            {
                Resolve = (ref int value, in (string, string) state) => value = state.Item1.GetHashCode() + state.Item2.GetHashCode(),
                State = ("Karl", "McTavish")
            };
            var dataloos = new byte[64];
            Unsafe.Copy(Unsafe.AsPointer(ref dataloos[0]), ref datalou);
            ref var dataloo = ref Unsafe.As<byte, Datalou>(ref dataloos[0]);
            var actual = 1;
            var expected = "Karl".GetHashCode() + "McTavish".GetHashCode();
            var valuePointer = Unsafe.AsPointer(ref actual);
            var statePointer = Unsafe.AsPointer(ref dataloo.State);
            dataloo.Resolve(valuePointer, statePointer);
        }

        unsafe struct Palionque
        {
            public int Current => *Pointer;

            public int Index;
            public int* Pointer;
        }

        unsafe static void SadisticalyUnsafe()
        {
            Palionque Create()
            {
                var value = new Palionque();
                value.Pointer = (int*)Unsafe.AsPointer(ref value.Index);
                return value;
            }

            var a = Create();
            var b = a;
            a.Index++;
            b.Index++;
        }

        static unsafe void* AsPointer<T>(ref T value)
        {
            var reference = __makeref(value);
            return *(void**)&reference;
        }

        static unsafe ref T ToRef<T>(void* pointer)
        {
            var span = new Span<T>(pointer, 1);
            return ref span[0];
        }

        static unsafe void DoesItAllocate()
        {
            Array a = new int[] { 1, 2, 3 };
            Array b = new int[] { 4, 5, 6 };
            while (true) Array.Copy(a, 0, b, 0, 2);
        }

        static void InParameters()
        {
            void Set<T>(in T source, T value)
            {
                Unsafe.AsRef(source) = value;
            }

            var position = new Position { };
            Set(position.X, 123);
        }

        static void Main()
        {
            // Poulah();
            for (int i = 0; i < 100; i++)
            {
                Benchmark(1_000);
                Benchmark(10_000);
                Benchmark(100_000);
                Benchmark(1_000_000);
            }
        }

        public readonly struct BobaData : ISystem
        {
            public readonly AllEntities Entities;
            public readonly AllComponents Components;
            public readonly Components<Position> Positions;
            public readonly Components<IsDead> IsDead;
            public readonly Emitter<OnMove> OnMove;
            public readonly Receiver<OnMove> OnMove2;
            [None(typeof(IsDead))]
            public readonly Group<Read<Velocity>> Group;
            public readonly FettData Fett;
            public readonly Reaction<OnMove> OnMove3;

            public void Run() { }
        }

        public readonly struct FettData : ISystem
        {
            public readonly AllEntities Entities;
            public readonly AllComponents Components;
            public readonly Components<Position> Positions;
            public readonly Components<IsDead> IsDead;
            public readonly Emitter<OnMove> OnMove;
            public readonly Receiver<OnMove> OnMove2;
            [None(typeof(IsDead))]
            public readonly Group<Write<Position>> Group;

            public void Run() => throw new NotImplementedException();
        }

        static void Measure(string name, Action test, int iterations)
        {
            test();
            test();
            test();

            // GC.Collect();
            // GC.WaitForFullGCComplete();
            // GC.WaitForPendingFinalizers();
            // GC.Collect();

            long total = 0;
            var minimum = long.MaxValue;
            var maximum = long.MinValue;
            var watch = new Stopwatch();
            for (var i = 0; i < iterations; i++)
            {
                watch.Restart();
                test();
                watch.Stop();
                total += watch.ElapsedTicks;
                minimum = Math.Min(minimum, watch.ElapsedTicks);
                maximum = Math.Max(maximum, watch.ElapsedTicks);
            }

            Console.WriteLine($"{name}   ->   Total: {TimeSpan.FromTicks(total)} | Average: {TimeSpan.FromTicks(total / iterations)} | Minimum: {TimeSpan.FromTicks(minimum)} | Maximum: {TimeSpan.FromTicks(maximum)}");
        }
    }
}
