using Entia.Core;
using Entia.Serialization;
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
        public class Cyclic { public Cyclic A; }
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
            cyclic.A = new Cyclic { A = new Cyclic { A = cyclic } };
            value[cyclic] = cyclic.A;
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
            world2.Container.Add(new Serializers.BlittableObject<Position>());
            world2.Container.Add(new Serializers.BlittableArray<Position>());
            world2.Container.Add(new Serializers.BlittableObject<Velocity>());
            world2.Container.Add(new Serializers.BlittableArray<Velocity>());
            world2.Container.Add(new Serializers.BlittableObject<Mass>());
            world2.Container.Add(new Serializers.BlittableArray<Mass>());
            world2.Container.Add(new Serializers.BlittableObject<Lifetime>());
            world2.Container.Add(new Serializers.BlittableArray<Lifetime>());
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

        static void Main()
        {
            TestFamilies();
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
