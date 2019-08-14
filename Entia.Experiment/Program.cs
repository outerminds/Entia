using Entia.Core;
using Entia.Experiment.Serialization;
using Entia.Experiment.Serializationz;
using Entia.Injectables;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Serialization;
using Entia.Nodes;
using Entia.Queryables;
using Entia.Systems;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using static Entia.Nodes.Node;

namespace Entia.Experiment
{
    [Serializable]
    public struct Position : IComponent { public float X, Y, Z; }
    [Serializable]
    public struct Velocity : IComponent { public float X, Y, Z; }
    [Serializable]
    public struct Lifetime : IComponent { public float Remaining; }
    public struct Targetable : IComponent { }
    public struct Targeter : IComponent { public Entity? Current; public float Distance; }
    [Serializable]
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
            // var result = world.Controllers().Run(node, () => !game[0].Quit);
        }

        static void Simple2()
        {
            var world = new World();
            var node = Node.Sequence(
                System<Systems.A>(),
                System<Systems.B>(),
                System<Systems.C>()
            );

            // if (world.Controllers().Control(node).TryValue(out var controller))
            // {
            //     ref var game = ref world.Resources().Get<Game>();
            //     controller.Initialize();
            //     while (!game.Quit) controller.Run();
            //     controller.Dispose();
            // }
        }

        static void TypeMap()
        {
            var (super, sub) = (false, false);
            var map = new TypeMap<IEnumerable, string>();
            map.Set<List<int>>("Poulah");
            map.Set<List<string>>("Viarge");
            map.Set<IList>("Jango");
            var value1 = map.Get(typeof(List<>), out var success1, super, sub);
            var value2 = map.Get(typeof(IList), out var success2, super, sub);
            var value3 = map.Get(typeof(List<>), out var success3, super, sub);
            var value4 = map.Get<IList>(out var success4, super, sub);
            var value5 = map.Get(typeof(List<string>), out var success5, super, sub);
            var value6 = map.Get(typeof(IList<string>), out var success6, super, sub);
            var value7 = map.Get<IList<string>>(out var success7, super, sub);
            map.Remove<List<int>>(super, sub);
            var value8 = map.Get(typeof(IList), out var success8, super, sub);
            var value9 = map.Get<IList>(out var success9, super, sub);
        }

        class Shiatsi { public ulong A; }
        static void TestLaPool()
        {
            var pool = new Nursery<Shiatsi>(() => new Shiatsi(), boba => boba.A++);

            for (var i = 0; i < 10; i++)
            {
                var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() => Enumerable.Range(0, 100_000).AsParallel().Select(index => pool.Allocate()).ToArray())).ToArray();
                var items = Task.WhenAll(tasks).Result.SelectMany(_ => _).ToArray();
                if (items.Distinct().Some().SequenceEqual(items)) Console.WriteLine($"YUUSSSS");
                else
                {
                    Console.WriteLine("SHEISSSSS");
                    Console.ReadKey();
                }
                pool.Free();
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

        unsafe static void PleaseStopThisMadness()
        {
            TTarget Cast1<TSource, TTarget>(ref TSource source)
            {
                var sourceReference = __makeref(source);
                var target = default(TTarget);
                var targetReference = __makeref(target);
                *(IntPtr*)&targetReference = *(IntPtr*)&sourceReference;
                return __refvalue(targetReference, TTarget);
            }

            T Cast2<T>(ref byte source)
            {
                var sourceReference = __makeref(source);
                var target = default(T);
                var targetReference = __makeref(target);
                *(IntPtr*)&targetReference = *(IntPtr*)&sourceReference;
                return __refvalue(targetReference, T);
            }

            void Cast3<TSource, TTarget>(ref TSource source, ref TTarget target)
            {
                var sourceReference = __makeref(source);
                var targetReference = __makeref(target);
                *(IntPtr*)&targetReference = *(IntPtr*)&sourceReference;
                target = __refvalue(targetReference, TTarget);
            }

            var position1 = new Position { X = 1, Y = 2, Z = 3 };
            var velocity1 = Cast1<Position, Velocity>(ref position1);
            velocity1.X++;

            var data2 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var velocity2 = Cast2<Velocity>(ref data2[0]);

            var position3 = new Position { X = 1, Y = 2, Z = 3 };
            var velocity3 = new Velocity();
            Cast3(ref position3, ref velocity3);
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

        static void DebugView()
        {
            var world1 = new World();
            world1.Resources().Set(new Resources.Debug { Name = "World1" });
            var world2 = new World();
            var random = new Random();


            for (int i = 0; i < 100; i++)
            {
                var world = random.NextDouble() < 0.5 ? world1 : world2;
                var entity = world.Entities().Create();
                world.Components().Set(entity, new Components.Debug { Name = $"Index {i}" });

                if (random.NextDouble() < 0.5)
                    world.Components().Set(entity, new Position { X = i + 1, Y = i + 2, Z = i + 3 });
                else
                    world.Components().Set(entity, new Velocity { X = i + 1, Y = i + 2, Z = i + 3 });
            }

            var entities = world1.Entities().ToArray();

            byte[] Allocate(int amount) => new byte[amount];

            for (int i = 0; i < 1000; i++)
            {
                Allocate(4096);
                world1 = world2 = new World();
                Allocate(8192);
            }

            var instances = World.Instances();
        }

        static void Performance()
        {
            var count = 0;
            void Run()
            {
                var world = new World();
                var entities = world.Entities();
                while (true)
                {
                    var entity = entities.Create();
                    entities.Destroy(entity);
                    world.Resolve();
                    count++;
                }
            }
            Task.Run(Run);

            var timer = new Timer(_ =>
            {
                var current = count;
                count = 0;
                Console.WriteLine(current);
            }, default, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            while (true) { }
        }

        public unsafe struct QueryC : Queryables.IQueryable
        {
            public Velocity* P2;
            public Position* P1;
            public Velocity* P3;
            public byte A1, A2, A3;
            public Position* P4;
            public ushort B1, B2;
            public Velocity* P5;
            public uint C1, C2, C3;
            public Position* P6;
            public Entity Entity;
            public Any<Read<Position>, Write<Velocity>> Any;
            public bool D1, D2, D3;
            public Velocity* P7;
            public Position* P8;
        }
        static unsafe void Layout()
        {
            // int IndexOf(IntPtr pointer, int size)
            // {
            //     var bytes = (byte*)pointer;
            //     for (int i = 0; i < size; i++) if (bytes[i] == 0) return i;
            //     return -1;
            // }

            (FieldInfo, int)[] For<T>()
            {
                var fields = typeof(T).InstanceFields();
                var size = UnsafeUtility.Size<T>();
                var count = size / sizeof(uint);
                var bytes = new byte[size];
                bytes.Fill(byte.MaxValue);
                var instance = UnsafeUtility.As<byte, T>(ref bytes[0]);
                var layout = new (FieldInfo field, int offset)[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    object box = instance;
                    field.SetValue(box, default);
                    // var offset = IndexOf(UnsafeUtility.Unbox(box), size);
                    // layout[i] = (field, offset);
                }
                Array.Sort(layout, (a, b) => a.offset.CompareTo(b.offset));
                return layout;
            }

            For<QueryC>();
        }

        static unsafe void SuperUnsafe()
        {
            var positions = new Position[32];
            var array = positions as Array;
            var target = (void*)UnsafeUtility.AsPointer(ref positions[0]);
            // var bytes = new byte[positions.Length * UnsafeUtility.Size<Position>()];
            // for (int i = 0; i < bytes.Length; i++) bytes[i] = 19;
            // fixed (byte* source = bytes) Buffer.MemoryCopy(source, target, bytes.Length, bytes.Length);

            var size = positions.Length * UnsafeUtility.Size<Position>();
            var @string = new string('a', size / sizeof(char));
            fixed (char* source = @string) Buffer.MemoryCopy(source, target, size, size);
        }

        static void TestJson()
        {
            var text = @"{ ""a "": [4.4
            ,
            ""aofiu"", true, { ""b"": null }]    }";
            var node = Json.Parse(text);
        }

        public class Cyclic { public Cyclic A; }
        static void Serializer1()
        {
            var world = new World();
            var entities = world.Entities();
            var messages = world.Messages();
            var resources = world.Resources();
            var boxes = world.Boxes();
            var components = world.Components();
            for (int i = 0; i < 10; i++) entities.Create();
            var serializers = world.Serializers();
            var cyclic = new Cyclic(); cyclic.A = cyclic;
            byte[] bytes;

            serializers.Serialize(cyclic, out bytes);
            serializers.Deserialize(bytes, out Cyclic b);

            serializers.Serialize((object)cyclic, out bytes);
            serializers.Deserialize(bytes, out Cyclic c);

            serializers.Serialize(((object)13, (object)27), out bytes);
            serializers.Deserialize(bytes, out (object, object) tuple1);

            serializers.Serialize((object)((object)13, (object)27), out bytes);
            serializers.Deserialize(bytes, out object tuple2);

            serializers.Serialize((new Entity(1, 2), new Entity(10, 11)), out bytes);
            serializers.Deserialize(bytes, out (Entity, Entity) tuple3);

            serializers.Serialize(new Entity[] { new Entity(1, 2), new Entity(10, 11) }, out bytes);
            serializers.Deserialize(bytes, out Entity[] array);

            var index = 0;
            var action = new Action(() => index++);
            action();
            serializers.Serialize(action, out bytes, action.Target);
            serializers.Deserialize(bytes, out action, action.Target);
            action();

            var inAction = new RefAction<int>((ref int value) => value++);
            serializers.Serialize(inAction, out bytes);
            serializers.Deserialize(bytes, out inAction);
            inAction(ref index);

            var reaction = new Entia.Modules.Message.Reaction<OnCreate>();
            reaction.Add((in OnCreate message) => { });
            serializers.Serialize(reaction, out bytes);
            serializers.Deserialize(bytes, out reaction);

            serializers.Serialize(entities, out bytes);
            serializers.Deserialize(bytes, out entities);
            serializers.Serialize((object)entities, out bytes);
            serializers.Deserialize(bytes, out entities);

            // serializers.Serialize(components, out bytes);
            // serializers.Deserialize(bytes, out Modules.Components f);

            serializers.Serialize(boxes, out bytes);
            serializers.Deserialize(bytes, out boxes);

            serializers.Serialize(messages, out bytes);
            serializers.Deserialize(bytes, out messages);

            serializers.Serialize(resources, out bytes);
            serializers.Deserialize(bytes, out resources);

            serializers.Serialize(world, out bytes);
            serializers.Deserialize(bytes, out world);
        }

        static void Serializer2()
        {
            var world = new World();
            byte[] bytes;
            bool success;

            var type = typeof(Program);
            success = world.Serialize(type, type.GetType(), out bytes);
            success = world.Deserialize(bytes, out type);

            var array = new int[] { 1, 2, 3, 4 };
            success = world.Serialize(array, out bytes);
            success = world.Deserialize(bytes, out array);

            var cycle = new Cyclic();
            cycle.A = cycle;
            success = world.Serialize(cycle, out bytes);
            success = world.Deserialize(bytes, out cycle);

            success = world.Serialize(null, typeof(object), out bytes);
            success = world.Deserialize(bytes, out object @null);

            var function = new Func<int>(() => 321);
            success = world.Serialize(function, out bytes);
            success = world.Deserialize(bytes, out function);
            var value = function();

            var action = new Action(() => value += 1);
            success = world.Serialize(action, out bytes, action.Target);
            success = world.Deserialize(bytes, out action, action.Target);
            action();

            var reaction = new Entia.Modules.Message.Reaction<OnCreate>();
            success = world.Serialize(reaction, out bytes);
            success = world.Deserialize(bytes, out reaction);

            var emitter = new Entia.Modules.Message.Emitter<OnCreate>();
            success = world.Serialize(emitter, out bytes);
            success = world.Deserialize(bytes, out emitter);

            var entities = world.Entities();
            for (int i = 0; i < 100; i++) entities.Create();
            success = world.Serialize(entities, out bytes);
            success = world.Deserialize(bytes, out entities);
        }

        static void CompareSerializers()
        {
            // Serializer.Object(
            //     () => new World(),
            //     Serializer.Member.Property(
            //         (in World world) => world.ToArray(),
            //         (ref World world, in IModule[] modules) => { for (int i = 0; i < modules.Length; i++) world.Set(modules[i]); })
            // );
            // Serializer.Blittable.Object<Entity>();
            // Serializer.Blittable.Array<Entity>();
            // Serializer.Object(
            //     () => new List<Entity>(),
            //     Serializer.Member.Property(
            //         (in List<Entity> list) => list.ToArray(),
            //         (ref List<Entity> list, in Entity[] values) => list.AddRange(values))
            // );
            // Serializer.Object(
            //     () => new Entity(),
            // );
            // var value = (new Position[1000], new Velocity[1000], new Mass[1000]);
            // var value = new Dictionary<object, object>();
            // value[1] = "2";
            // value["3"] = 4;
            // value[DateTime.Now] = TimeSpan.MaxValue;
            // value[TimeSpan.MinValue] = DateTime.UtcNow;
            // value[new object()] = value;
            // value[new Position[100]] = new Velocity[100];
            // value[new List<Mass>(new Mass[100])] = new List<Lifetime>(new Lifetime[100]);
            // CompareSerializers(value);
            var values = new Dictionary<Position, Velocity>();
            for (int i = 0; i < 100; i++) values[new Position { X = i }] = new Velocity { Y = 1 };
            CompareSerializers((values, new List<Position>(new Position[100]), new List<Velocity>(new Velocity[100])));
            // CompareSerializers(new List<Position>(new Position[1000]));
        }
        static void CompareSerializers<T>(T value)
        {
            var container = new Container();
            var world = new World(container);

            world.Describe(value, out var description);
            world.Serialize(value, out var bytes2);

            byte[] bytes3;
            var binary = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                binary.Serialize(stream, value);
                bytes3 = stream.ToArray();
            }

            void Describe() => world.Describe(value, out _);
            void Instantiate() => world.Instantiate(description, out T _);
            void NewSerialize() => world.Serialize(value, out _);
            void NewDeserialize() => world.Deserialize(bytes2, out T _);
            void BinarySerialize()
            {
                using (var stream = new MemoryStream())
                {
                    binary.Serialize(stream, value);
                    stream.ToArray();
                }
            }
            void BinaryDeserialize()
            {
                using (var stream = new MemoryStream(bytes3)) binary.Deserialize(stream);
            }

            while (true)
            {
                Test.Measure(NewSerialize, new Action[] { BinarySerialize, Describe }, 1000);
                Test.Measure(NewDeserialize, new Action[] { BinaryDeserialize, Instantiate }, 1000);
                Console.WriteLine();
            }
        }

        interface IMambo : ITrait { }
        interface INumba : ITrait { }
        interface IFive : ITrait, IImplementation<string, Karlz.C>, IImplementation<object, Five> { }
        sealed class Five : IFive { }
        struct Karlz : IImplementation<Karlz.B>
        {
            [Implementation]
            public sealed class A : IMambo { }
            public sealed class B : INumba { }
            public sealed class C : IFive { }

            // [Implementation]
            static readonly C D = new C();
        }

        static void Traitz()
        {
            Container.TryDefault<Karlz, IMambo>(out var mambo);
            Container.TryDefault<Karlz, INumba>(out var numba);
            Container.TryDefault<Karlz, IFive>(out var five);
            Container.TryDefault<string, IFive>(out var b);
        }

        static void PoulahDescriptor()
        {
            var container = new Container();
            var world = new World(container);

            void Test<T>(T value)
            {
                world.Describe(value, out var description);
                world.Instantiate(description, out value);
            }

            Test(new List<int> { });
            Test((object)new List<int> { });
            Test(new { A = 1, B = 2 });
            Test(new[] { 0, 1, 2, 3 });
            Test(new object[] { 0, "A", DateTime.Now, TimeSpan.MaxValue, new List<int> { 1, 2, 3 } });
            var cyclic = new Cyclic();
            cyclic.A = cyclic;
            Test(cyclic);
        }

        static void TypeMapBenchmark()
        {
            var dictionary = new Dictionary<Type, object>();
            var intDictionary = new Dictionary<int, object>();
            var concurrent = new ConcurrentDictionary<Type, object>();
            var map = new TypeMap<object, object>();
            var index = map.Index<int>();
            var value = default(object);
            void Add<T>(T current)
            {
                intDictionary[map.Index<T>()] = concurrent[typeof(T)] = dictionary[typeof(T)] = current; map[typeof(T)] = current;
            }
            Add(byte.MaxValue);
            Add(sbyte.MaxValue);
            Add(ushort.MaxValue);
            Add(short.MaxValue);
            Add(uint.MaxValue);
            Add(int.MaxValue);
            Add(ulong.MaxValue);
            Add(long.MaxValue);
            Add(float.MaxValue);
            Add(double.MaxValue);
            Add(decimal.MaxValue);
            Add(new object());
            Add(new Unit());
            Add(new Cyclic());
            Add(DateTime.MaxValue);
            Add(TimeSpan.MaxValue);
            Add(dictionary);
            Add(intDictionary);
            Add(concurrent);
            Add(map);
            var array = map.Values.ToArray();

            void ArrayGet() => value = array[index];
            void DictionaryIndexer<T>() => value = dictionary[typeof(T)];
            void DictionaryTryGet<T>() => dictionary.TryGetValue(typeof(T), out value);
            void IntDictionaryIndexer<T>() => value = intDictionary[map.Index<T>()];
            void IntDictionaryTryGet<T>() => intDictionary.TryGetValue(map.Index<T>(), out value);
            void IntDictionaryTryGetIndex() => intDictionary.TryGetValue(index, out value);
            void ConcurrentIndexer<T>() => value = concurrent[typeof(T)];
            void ConcurrentTryGet<T>() => concurrent.TryGetValue(typeof(T), out value);
            void MapIndexer<T>() => value = map[typeof(T)];
            void MapGet<T>() => value = map.Get<T>(out _);
            void MapGetIndex() => value = map[index];
            void MapTryGet<T>() => map.TryGet(typeof(T), out value);
            void MapTryGetT<T>() => map.TryGet<T>(out value);
            void MapTryGetIndex() => map.TryGet(index, out value);

            while (true)
            {
                Test.Measure(DictionaryIndexer<int>, new Action[]
                {
                    ArrayGet,
                    DictionaryTryGet<int>,
                    DictionaryTryGet<Action>,
                    IntDictionaryIndexer<int>,
                    IntDictionaryTryGet<int>,
                    IntDictionaryTryGet<Action>,
                    IntDictionaryTryGetIndex,
                    ConcurrentIndexer<int>,
                    ConcurrentTryGet<int>,
                    ConcurrentTryGet<Action>,
                    MapIndexer<int>,
                    MapGet<int>,
                    MapGet<Action>,
                    MapGetIndex,
                    MapTryGet<int>,
                    MapTryGet<Action>,
                    MapTryGetT<int>,
                    MapTryGetT<Action>,
                    MapTryGetIndex
                }, 100_000);
                Console.WriteLine();
            }
        }

        static void Main()
        {
            // TypeMapBenchmark();
            // Traitz();
            // PoulahDescriptor();
            // Serializer1();
            Serializer2();
            CompareSerializers();
            // SuperUnsafe();
            // Performance();
            // Layout();
            // DebugView();
            // ComponentTest.Run();
            // ParallelTest.Run();
            // TypeMap();

            // Group3Test.Benchmark(1_000);
            // Group3Test.Benchmark(10_000);
            // Group3Test.Benchmark(100_000);
            // Group3Test.Benchmark(1_000_000);

            // Group2Test.Benchmark(1_000);
            // Group2Test.Benchmark(10_000);
            // Group2Test.Benchmark(100_000);
            // Group2Test.Benchmark(1_000_000);

            // TestLaPool();
            // for (int i = 0; i < 1000; i++)
            // {
            //     GroupTest.Benchmark(1000 * i);
            // GroupTest.Benchmark(1_000);
            // GroupTest.Benchmark(10_000);
            // GroupTest.Benchmark(100_000);
            // GroupTest.Benchmark(1_000_000);
            // }
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
    }
}
