using Entia.Core;
using Entia.Experimental.Serialization;
using Entia.Injectables;
using Entia.Messages;
using Entia.Modules;
using Entia.Queryables;
using Entia.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Entia.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Entia.Core.Documentation;

//[TypeDiagnostic("Poulah '{type}'", WithAnyFilters = Filters.Types, HaveNoneFilters = Filters.Class)]
[TypeDiagnostic("Type '{type}' must implement 'ISwanson'", WithAnyFilters = Filters.Class, HaveAllImplementations = new[] { typeof(ISwanson) })]
[TypeDiagnostic("Type '{type}' must have attribute 'Attribz'", WithAnyFilters = Filters.Class, HaveAllAttributes = new[] { typeof(Attribz) })]
[TypeDiagnostic("Type '{type}' must be public.", WithAnyFilters = Filters.Class, HaveAllFilters = Filters.Public)]
interface IKarl { }
interface ISwanson { }
class Attribz : Attribute { }
 class Karl : IKarl { }

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

        [Serializable]
        public class Cyclic
        {
            public Cyclic This;
            public Cyclic() { This = this; }
        }
        public struct BlittableA { public int A, B; }
        public struct NonBlittableA { public int A; public string B; }

        static void Serializer()
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
            cycle.This = cycle;
            success = world.Serialize(cycle, out bytes);
            success = world.Deserialize(bytes, out cycle);

            success = world.Serialize(null, typeof(object), out bytes);
            success = world.Deserialize(bytes, out object @null);

            var function = new Func<int>(() => 321);
            success = world.Serialize(function, out bytes);
            success = world.Deserialize(bytes, out function);
            var value = function();

            var action = new Action(() => value += 1);
            success = world.Serialize(action, out bytes, default, action.Target);
            success = world.Deserialize(bytes, out action, default, action.Target);
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

            success = world.Serialize(world, out bytes);
            success = world.Deserialize(bytes, out world);

            success = world.Serialize(new NonBlittableA { A = 1, B = "Boba" }, out bytes);
            success = world.Deserialize(bytes, out BlittableA ba);

            success = world.Serialize(new BlittableA { A = 1, B = 2 }, out bytes);
            success = world.Deserialize(bytes, out NonBlittableA nba);
        }

        static void CompareSerializers()
        {
            const int size = 10;
            var value = new Dictionary<object, object>();
            value[1] = "2";
            value["3"] = 4;
            value[DateTime.Now] = TimeSpan.MaxValue;
            value[TimeSpan.MinValue] = DateTime.UtcNow;
            value[new object()] = value;
            value[new Position[size]] = new Velocity[size];
            value[new List<Mass>(new Mass[size])] = new List<Lifetime>(new Lifetime[size]);
            var cyclic = new Cyclic();
            cyclic.This = new Cyclic { This = new Cyclic { This = cyclic } };
            value[cyclic] = cyclic.This;
            value[new string[] { "Boba", "Fett", "Jango" }] = new byte[512];

            var values = new Dictionary<Position, Velocity>();
            for (int i = 0; i < size; i++) values[new Position { X = i }] = new Velocity { Y = 1 };
            value[(1, "2", byte.MaxValue, short.MinValue)] = (values, new List<Position>(new Position[size]), new List<Velocity>(new Velocity[size]));

            CompareSerializers(value);
        }
        static void CompareSerializers<T>(T value)
        {
            var world1 = new World();
            var world2 = new World();
            world2.Container.Add(new Experimental.Serializers.BlittableObject<Position>());
            world2.Container.Add(new Experimental.Serializers.BlittableArray<Position>());
            world2.Container.Add(new Experimental.Serializers.BlittableObject<Velocity>());
            world2.Container.Add(new Experimental.Serializers.BlittableArray<Velocity>());
            world2.Container.Add(new Experimental.Serializers.BlittableObject<Mass>());
            world2.Container.Add(new Experimental.Serializers.BlittableArray<Mass>());
            world2.Container.Add(new Experimental.Serializers.BlittableObject<Lifetime>());
            world2.Container.Add(new Experimental.Serializers.BlittableArray<Lifetime>());
            world1.Serialize(value, out var bytes1, Options.Blittable);
            world1.Serialize(value, out var bytes2);
            world2.Serialize(value, out var bytes3);

            byte[] bytes4;
            var binary = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                binary.Serialize(stream, value);
                bytes4 = stream.ToArray();
            }

            void BlittableSerialize() => world1.Serialize(value, out var a, Options.Blittable);
            void BlittableDeserialize() => world1.Deserialize(bytes1, out T a, Options.Blittable);
            void NoneSerialize() => world1.Serialize(value, out var a);
            void NoneDeserialize() => world1.Deserialize(bytes2, out T a);
            void ManualSerialize() => world2.Serialize(value, out var a);
            void ManualDeserialize() => world2.Deserialize(bytes3, out T a);
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
                using (var stream = new MemoryStream(bytes4)) binary.Deserialize(stream);
            }

            while (true)
            {
                Test.Measure(BlittableSerialize, new Action[] { NoneSerialize, ManualSerialize, BinarySerialize }, 1000);
                Test.Measure(BlittableDeserialize, new Action[] { NoneDeserialize, ManualDeserialize, BinaryDeserialize }, 1000);
                Console.WriteLine();
            }
        }

        static void TestFamilies()
        {
            var world = new World();
            var entities = world.Entities();
            var families = world.Families();

            var parent = entities.Create();
            var middle = entities.Create();
            var child = entities.Create();
            var success1 = families.Adopt(parent, middle);
            var success2 = families.Adopt(middle, child);
            var success3 = families.Adopt(child, parent);
            var family = families.Family(parent).ToArray();
            var descendants = families.Descendants(parent).ToArray();
            var ancestors = families.Ancestors(child).ToArray();
        }

        struct Wrapper<T> { public T Value; }

        static unsafe void TestJson()
        {
            var container = new Container();

            var json1 = File.ReadAllText(@"C:\Projects\Ululab\Numbers\Assets\Resources\Creative\Levels\ARCHIVE~11_KIDS_OEP_A.json");
            var node1 = Json.Serialization.Parse(json1).Or(Node.Null);
            var json2 = Json.Serialization.Generate(node1, GenerateFormat.Indented);
            var node2 = Json.Serialization.Parse(json2).Or(Node.Null);
            var json3 = Json.Serialization.Generate(node1, GenerateFormat.Compact);
            var node3 = Json.Serialization.Parse(json3);

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.None,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            var twitterJson = File.ReadAllText(@"C:\Projects\C#\Entia\Entia.Experiment\Json\twitter.json");
            var twitterNode = Json.Serialization.Parse(twitterJson).Or(Node.Null);
            var twitterObject = JsonConvert.DeserializeObject<JObject>(twitterJson, settings);

            void Test<T>(in T value, out string json, out Node node, out T instance, ConvertOptions options = ConvertOptions.All)
            {
                json = Json.Serialization.Serialize(value, options, container: container);
                node = Json.Serialization.Parse(json).Or(Node.Null);
                instance = Json.Serialization.Instantiate<T>(node, container: container);
            }

            Test(new Cyclic(), out var json4, out var node4, out var value4);
            Test<object>(new Dictionary<object, object>
            {
                { 1, "2" },
                { "3", null },
                { 4, "5" },
                { DateTime.Now, TimeSpan.MaxValue }
            }, out var json5, out var node5, out var value5, ConvertOptions.Abstract);
            Test(new Dictionary<int, int> { { 1, -1 }, { 2, -2 } }, out var json6, out var node6, out var value6);
            Test<object>(1, out var json7, out var node7, out var value7);

            var dictionary = new Dictionary<object, object>();
            dictionary[1] = dictionary;
            Test(dictionary, out var json8, out var node8, out var value8);

            Test(new Stack<int>(new int[] { 1, 2, 3 }), out var json9, out var node9, out var value9);
            Test(new Queue<int>(new int[] { 1, 2, 3 }), out var json10, out var node10, out var value10);
            Test(new List<int>(new int[] { 1, 2, 3 }), out var json11, out var node11, out var value11);
            Test(new int[] { 1, 2, 3 }, out var json12, out var node12, out var value12);
            Test(new Queue(new object[] { 1, "2", 3L }), out var json13, out var node13, out var value13);
            Test(new SortedDictionary<int, int> { { 1, 2 }, { 3, 4 } }, out var json14, out var node14, out var value14);
            Test(new ConcurrentDictionary<int, int>(new[] { new KeyValuePair<int, int>(1, 2), new KeyValuePair<int, int>(3, 4) }), out var json15, out var node15, out var value15);

            void SerializeA<T>(T value)
            {
                var json = Json.Serialization.Serialize(new Wrapper<T> { Value = value }, container: container);
                value = Json.Serialization.Deserialize<Wrapper<T>>(json, container: container).OrDefault().Value;
            }

            void SerializeB<T>(T value)
            {
                var json = JsonConvert.SerializeObject(new Wrapper<T> { Value = value }, settings);
                value = JsonConvert.DeserializeObject<Wrapper<T>>(json, settings).Value;
            }

            string generated;
            void ParseA() => Json.Serialization.Parse(twitterJson);
            void ParseB() => JsonConvert.DeserializeObject<JObject>(twitterJson, settings);
            void GenerateA() => generated = Json.Serialization.Generate(twitterNode);
            void GenerateB() => generated = JsonConvert.SerializeObject(twitterObject, settings);
            void IntNumberA() => SerializeA(1);
            void ObjectNumberA() => SerializeA<object>(1);
            void CyclicA() => SerializeA(new Cyclic());
            void ObjectCyclicA() => SerializeA<object>(new Cyclic());
            void IntDictionaryA() => SerializeA(new Dictionary<int, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void IntObjectDictionaryA() => SerializeA(new Dictionary<int, object> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void ObjectIntDictionaryA() => SerializeA(new Dictionary<object, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void ObjectDictionaryA() => SerializeA(new Dictionary<object, object> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void LargeArrayA() => SerializeA(new ulong[256]);

            void IntNumberB() => SerializeB(1);
            void ObjectNumberB() => SerializeB(1);
            void CyclicB() => SerializeB(new Cyclic());
            void ObjectCyclicB() => SerializeB(new Cyclic());
            void IntDictionaryB() => SerializeB(new Dictionary<int, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void IntObjectDictionaryB() => SerializeB(new Dictionary<int, object> { { 1, TimeSpan.MaxValue }, { 2, TimeSpan.MaxValue }, { 3, TimeSpan.MaxValue } });
            void ObjectIntDictionaryB() => SerializeB(new Dictionary<object, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void ObjectDictionaryB() => SerializeB(new Dictionary<object, object> { { DateTime.Now, TimeSpan.MaxValue }, { DateTime.Now, TimeSpan.MaxValue }, { DateTime.Now, TimeSpan.MaxValue } });
            void LargeArrayB() => SerializeB(new ulong[256]);

            for (int i = 0; i < 25; i++)
            // while (true)
            {
                Experiment.Test.Measure(ParseA, new Action[]
                {
                    ParseB,
                    GenerateA,
                    GenerateB
                }, 100, 1);

                Console.WriteLine();
                Experiment.Test.Measure(IntNumberA, new Action[]
                {
                    ObjectNumberA,
                    CyclicA,
                    ObjectCyclicA,
                    IntDictionaryA,
                    IntObjectDictionaryA,
                    ObjectIntDictionaryA,
                    ObjectDictionaryA,
                    LargeArrayA,

                    IntNumberB,
                    ObjectNumberB,
                    CyclicB,
                    ObjectCyclicB,
                    IntDictionaryB,
                    IntObjectDictionaryB,
                    ObjectIntDictionaryB,
                    ObjectDictionaryB,
                    LargeArrayB,

                }, 1000);
                Console.WriteLine();
            }
        }

        delegate void Poulah<T1, T2>(Entity entity, ref T1 a, in T2 b);
        delegate void Poulah(IntPtr a, IntPtr b, IntPtr c);
        static void VeryUnsafe()
        {
            static void Method(Entity entity, ref Position position, in Velocity velocity)
            {
                Console.WriteLine("$Auiaofiu");
            }

            var entity = new Entity(1, 2);
            var positions = new Position[8];
            var velocities = new Velocity[8];

            var pointerA = UnsafeUtility.As<Entity, IntPtr>(ref entity);
            var pointerB = UnsafeUtility.AsPointer(ref positions[0]);
            var pointerC = UnsafeUtility.AsPointer(ref velocities[0]);

            var pointer = new Poulah<Position, Velocity>(Method);//.Method.MethodHandle.GetFunctionPointer();
            var function = UnsafeUtility.As<Poulah<Position, Velocity>, Poulah>(ref pointer);//Marshal.GetDelegateForFunctionPointer<Poulah>(pointer);
            function(pointerA, pointerB, pointerC);

            /*
            public static System Motion(World world)
            {
                world.Systems().Run((ref Time time) =>
                {

                });
                return world.Systems().RunEach((ref Position position, in Velocity velocity) =>
                {
                    position.X += velocity.X;
                    position.Y += velocity.Y;
                });
                return world.Systems().RunEach((ref Position position, ref Velocity velocity) =>
                {
                    position.X += velocity.X;
                    position.Y += velocity.Y;
                });
                return world.Systems().RunEach((Write<Position> position, Read<Velocity> velocity) =>
                {
                    position.Value.X += velocity.Value.X;
                    position.Value.Y += velocity.Value.Y;
                });
            }
            */
        }

        static void Main()
        {
            VeryUnsafe();
            // TestJson();
            // TestFamilies();
            // Serializer();
            // TypeMapTest.Benchmark();
            // CompareSerializers();
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
