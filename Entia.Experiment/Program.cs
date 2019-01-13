using Entia.Core;
using Entia.Injectables;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Nodes;
using Entia.Queryables;
using Entia.Queryables2;
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
    public struct IsDead : ITag { }
    public struct IsInvincible : ITag { }
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
            var oldComponents = world.Components();
            var newComponents = world.Components3();
            var random = new Random();
            var list = new List<Entity>();

            Entity RandomEntity() => list.Count == 0 ? Entity.Zero : list[random.Next(list.Count)];
            void AddComponent<T>(in T component) where T : struct, IComponent
            {
                var entity = RandomEntity();
                oldComponents.Set(entity, component);
                newComponents.Set(entity, component);
            }
            void RemoveComponent<T>() where T : struct, IComponent
            {
                var entity = RandomEntity();
                oldComponents.Remove<T>(entity);
                newComponents.Remove<T>(entity);
            }

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
                        if (value2 < 0.25) AddComponent(new Position { X = (float)value1, Y = (float)value2 });
                        else if (value2 < 0.5) AddComponent(new Velocity { X = (float)value1, Y = (float)value2 });
                        else if (value2 < 0.75) AddComponent(new Lifetime { Remaining = (float)value1 + (float)value2 });
                        else AddComponent(new Mass { Value = (float)value1 + (float)value2 });
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

            var oldGroup = world.Groups().Get(world.Queriers().Query<All<Write<Position>, Write<Velocity>>>());
            var oldArray = oldGroup.ToArray();
            var newGroup = new Entia.Modules.Group.Group3<All2<Write2<Position>, Write2<Velocity>>>(newComponents, world.Queriers2(), world.Messages());

            void ArrayIndexer()
            {
                for (int i = 0; i < oldArray.Length; i++)
                {
                    ref readonly var item = ref oldArray[i];
                    ref var position = ref item.Value1.Value;
                    ref var Velocity = ref item.Value2.Value;
                    position.X += Velocity.X;
                }
            }

            void ArrayForeach()
            {
                foreach (var item in oldArray)
                {
                    ref var position = ref item.Value1.Value;
                    ref var Velocity = ref item.Value2.Value;
                    position.X += Velocity.X;
                }
            }

            void OldForeach()
            {
                foreach (ref readonly var item in oldGroup)
                {
                    ref var position = ref item.Value1.Value;
                    ref var Velocity = ref item.Value2.Value;
                    position.X += Velocity.X;
                }
            }

            void OldIndexer()
            {
                for (int i = 0; i < oldGroup.Count; i++)
                {
                    ref readonly var item = ref oldGroup[i];
                    ref var position = ref item.Value1.Value;
                    ref var Velocity = ref item.Value2.Value;
                    position.X += Velocity.X;
                }
            }

            void OldParallel()
            {
                System.Threading.Tasks.Parallel.For(0, oldGroup.Count, index =>
                {
                    ref readonly var item = ref oldGroup[index];
                    ref var position = ref item.Value1.Value;
                    ref var Velocity = ref item.Value2.Value;
                    position.X += Velocity.X;
                });
            }

            void NewForeach()
            {
                foreach (var item in newGroup)
                {
                    ref var position = ref item.Value1.Value;
                    ref var Velocity = ref item.Value2.Value;
                    position.X += Velocity.X;
                }
            }

            void NewTask()
            {
                Task.WhenAll(newGroup.Split(newGroup.Count / Environment.ProcessorCount).Select(split => Task.Run(() =>
                {
                    foreach (var item in split)
                    {
                        ref var position = ref item.Value1.Value;
                        ref var Velocity = ref item.Value2.Value;
                        position.X += Velocity.X;
                    }
                }))).Wait();
            }

            void NewParallel()
            {
                System.Threading.Tasks.Parallel.ForEach(newGroup.Split(newGroup.Count / Environment.ProcessorCount), split =>
                {
                    foreach (var item in split)
                    {
                        ref var position = ref item.Value1.Value;
                        ref var Velocity = ref item.Value2.Value;
                        position.X += Velocity.X;
                    }
                });
            }

            Action[] tests =
            {
                () => Measure($"Array Foreach ({oldGroup.Count})", ArrayForeach, 1000),
                () => Measure($"Array Indexer ({oldGroup.Count})", ArrayIndexer, 1000),
                () => Measure($"Old Foreach ({oldGroup.Count})", OldForeach, 1000),
                () => Measure($"Old Indexer ({oldGroup.Count})", OldIndexer, 1000),
                () => Measure($"New Foreach ({newGroup.Count})", NewForeach, 1000),
                () => Measure($"Old Parallel.For ({oldGroup.Count})", OldParallel, 1000),
                () => Measure($"New Task.WhenAll ({newGroup.Count})", NewTask, 1000),
                () => Measure($"New Parallel.ForEach ({newGroup.Count})", NewParallel, 1000)
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
                    world.Components3().Set(entity, new Position { X = (float)value });
                else if (value < 0.666)
                    world.Components3().Set(entity, new Velocity { X = (float)value });
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

            var group = world.Groups3().Get3<Write2<Position>>();
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
            var split = group.Split(10);
            var segments = split.Select(segment => segment.ToArray()).ToArray();
            world.Resolve();

            for (var i = 1; i <= iterations; i++)
            {
                var entity = world.Entities().Create();

                var has1 = world.Components3().Has<Position>(entity);
                var add1 = world.Components3().Set(entity, new Position { X = i });
                ref var write1 = ref world.Components3().Get<Position>(entity);
                write1.X++;

                var add2 = world.Components3().Set(entity, new Position { Y = i });
                var has2 = world.Components3().Has<Position>(entity);
                world.Resolve();
                var has3 = world.Components3().Has<Position>(entity);
                var add3 = world.Components3().Set(entity, new Position { Z = i });
                var add4 = world.Components3().Set(entity, new Velocity { X = i });
                var get1 = world.Components3().Get(entity).ToArray();
                world.Resolve();
                var get2 = world.Components3().Get(entity).ToArray();

                ref var write2 = ref world.Components3().Get<Position>(entity);
                write2.Y++;

                var has4 = world.Components3().Has<Position>(entity);
                var has5 = world.Components3().Has<Velocity>(entity);
                var remove1 = world.Components3().Remove<Position>(entity);
                world.Resolve();
                var has6 = world.Components3().Has<Velocity>(entity);
                ref var write3 = ref world.Components3().Get<Velocity>(entity);
                write3.X++;
                var clear1 = world.Components3().Clear(entity);
                var get3 = world.Components3().Get(entity).ToArray();
                var add5 = world.Components3().Set(entity, new Position { X = i + 10 });
                var has7 = world.Components3().Has<Position>(entity);
                var has8 = world.Components3().Has<Velocity>(entity);
                world.Resolve();

                var get4 = world.Components3().Get(entity).ToArray();
                var has9 = world.Components3().Has<Velocity>(entity);
                ref var write4 = ref world.Components3().Get<Position>(entity);
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
            public readonly Group<Entity> Group1;
            [None(typeof(IsDead))]
            public readonly Group<Entity> Group2;

            public readonly Query<Entity> Query1;
            [None(typeof(IsDead))]
            public readonly Query<Entity> Query2;

            public readonly AllEntities Entities;
            public readonly AllComponents Components;
            public readonly AllTags Tags;
            public readonly Components<Position> Positions;
            public readonly Tags<IsDead> IsDead;
            public readonly Emitter<OnMove> OnMove;
            public readonly Receiver<OnMove> OnMove2;
            [None(typeof(IsDead))]
            public readonly Group<All<Entity, Read<Velocity>>> Group;
            public readonly FettData Fett;
            public readonly Reaction<OnMove> OnMove3;

            public void Run() { }
        }

        public readonly struct FettData : ISystem
        {
            public readonly AllEntities Entities;
            public readonly AllComponents Components;
            public readonly AllTags Tags;
            public readonly Components<Position> Positions;
            public readonly Tags<IsDead> IsDead;
            public readonly Emitter<OnMove> OnMove;
            public readonly Receiver<OnMove> OnMove2;
            [None(typeof(IsDead))]
            public readonly Group<All<Entity, Write<Position>>> Group;
            public readonly Group<Entity> Group2;

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
