using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entia.Core;
using Entia.Core.Providers;
using Entia.Experiment.Serializationz;
using static Entia.Experiment.Serializers.Serializer;

namespace Entia.Experiment.Serializers
{
    [Implementation(typeof(List<>), typeof(ConcreteList<>))]
    [Implementation(typeof(Dictionary<,>), typeof(ConcreteDictionary<,>))]
    [Implementation(typeof(ValueTuple<,>), typeof(ConcreteTuple<,>))]
    [Implementation(typeof(ValueTuple<,,>), typeof(ConcreteTuple<,,>))]
    [Implementation(typeof(ValueTuple<,,,>), typeof(ConcreteTuple<,,,>))]
    [Implementation(typeof(ValueTuple<,,,,>), typeof(ConcreteTuple<,,,,>))]
    [Implementation(typeof(ValueTuple<,,,,,>), typeof(ConcreteTuple<,,,,,>))]
    [Implementation(typeof(ValueTuple<,,,,,,>), typeof(ConcreteTuple<,,,,,,>))]
    public interface ISerializer : ITrait,
        IImplementation<bool, BlittableObject<bool>>, IImplementation<bool[], BlittableArray<bool>>,
        IImplementation<char, BlittableObject<char>>, IImplementation<char[], BlittableArray<char>>,
        IImplementation<byte, BlittableObject<byte>>, IImplementation<byte[], BlittableArray<byte>>,
        IImplementation<sbyte, BlittableObject<sbyte>>, IImplementation<sbyte[], BlittableArray<sbyte>>,
        IImplementation<ushort, BlittableObject<ushort>>, IImplementation<ushort[], BlittableArray<ushort>>,
        IImplementation<short, BlittableObject<short>>, IImplementation<short[], BlittableArray<short>>,
        IImplementation<uint, BlittableObject<uint>>, IImplementation<uint[], BlittableArray<uint>>,
        IImplementation<int, BlittableObject<int>>, IImplementation<int[], BlittableArray<int>>,
        IImplementation<ulong, BlittableObject<ulong>>, IImplementation<ulong[], BlittableArray<ulong>>,
        IImplementation<long, BlittableObject<long>>, IImplementation<long[], BlittableArray<long>>,
        IImplementation<float, BlittableObject<float>>, IImplementation<float[], BlittableArray<float>>,
        IImplementation<double, BlittableObject<double>>, IImplementation<double[], BlittableArray<double>>,
        IImplementation<decimal, BlittableObject<decimal>>, IImplementation<decimal[], BlittableArray<decimal>>,
        IImplementation<DateTime, BlittableObject<DateTime>>, IImplementation<DateTime[], BlittableArray<DateTime>>,
        IImplementation<TimeSpan, BlittableObject<TimeSpan>>, IImplementation<TimeSpan[], BlittableArray<TimeSpan>>,

        IImplementation<string, ConcreteString>,
        IImplementation<Assembly, AbstractAssembly>,
        IImplementation<Module, AbstractModule>,
        IImplementation<Type, AbstractType>,
        IImplementation<MethodInfo, AbstractMethod>,
        IImplementation<MemberInfo, AbstractMember>,

        IImplementation<Unit, BlittableObject<Unit>>, IImplementation<Unit[], BlittableArray<Unit>>,
        IImplementation<object, Default>
    {
        bool Serialize(object instance, in SerializeContext context);
        bool Instantiate(out object instance, in DeserializeContext context);
        bool Initialize(ref object instance, in DeserializeContext context);
    }

    public abstract class Serializer<T> : ISerializer
    {
        public abstract bool Serialize(in T instance, in SerializeContext context);
        public bool Deserialize(out T instance, in DeserializeContext context) =>
            Instantiate(out instance, context) && Initialize(ref instance, context);
        public abstract bool Instantiate(out T instance, in DeserializeContext context);
        public abstract bool Initialize(ref T instance, in DeserializeContext context);

        bool ISerializer.Serialize(object instance, in SerializeContext context) => Serialize((T)instance, context);

        bool ISerializer.Instantiate(out object instance, in DeserializeContext context)
        {
            if (Instantiate(out var casted, context))
            {
                instance = casted;
                return true;
            }
            instance = default;
            return false;
        }

        bool ISerializer.Initialize(ref object instance, in DeserializeContext context)
        {
            var casted = (T)instance;
            if (Initialize(ref casted, context))
            {
                instance = casted;
                return true;
            }
            instance = default;
            return false;
        }
    }

    public sealed class Default : Provider<ISerializer>
    {
        public override IEnumerable<ISerializer> Provide(Type type)
        {
            ISerializer Create()
            {
                var data = TypeUtility.GetData(type);
                if (type.IsArray)
                {
                    var element = TypeUtility.GetData(data.Element);
                    if (element.Size is int size) return Blittable.Array(element, size);
                    else return Array(element);
                }
                else if (data.Size is int size) return Blittable.Object(data, size);
                else if (type.Is<Delegate>()) return Delegate(type);
                else if (type.Is(typeof(List<>))) return List(data.Arguments[0]);
                else if (type.Is(typeof(Dictionary<,>))) return Dictionary(data.Arguments[0], data.Arguments[1]);
                else return Object(type);
            }

            yield return Create();
        }
    }

    public static class Serializer
    {
        public static class Member
        {
            public static IMember<T> Field<T, TValue>(Field<T, TValue>.Getter get) => new Field<T, TValue>(get);
            public static IMember<T> Property<T, TValue>(Property<T, TValue>.Getter get, Property<T, TValue>.Setter set) => new Property<T, TValue>(get, set);
            public static IMember Reflection(FieldInfo field, ISerializer serializer = null) => new Experiment.Reflection(field);
            public static IMember Reflection(PropertyInfo property, ISerializer serializer = null) => new Experiment.Reflection(property);
        }

        public static class Blittable
        {
            public static Serializer<(T[] items, int count)> PairArray<T>() where T : unmanaged => Map(
                (in (T[], int) pair) => pair.ToArray(),
                (in T[] items) => (items, items.Length),
                Array<T>());

            public static Serializer<T[]> Array<T>() where T : unmanaged => new BlittableArray<T>();
            public static ISerializer Array(Type type, int size) => new BlittableArray(type, size);
            public static Serializer<T> Object<T>() where T : unmanaged => new BlittableObject<T>();
            public static ISerializer Object(Type type, int size) => new BlittableObject(type, size);
        }

        public static class Reflection
        {
            public static Serializer<Assembly> Assembly() => new AbstractAssembly();
            public static Serializer<Module> Module() => new AbstractModule();
            public static Serializer<Type> Type() => new AbstractType();
            public static Serializer<MethodInfo> Method() => new AbstractMethod();
            public static Serializer<MemberInfo> Member() => new AbstractMember();
        }

        public static Serializer<TFrom> Map<TFrom, TTo>(InFunc<TFrom, TTo> to, InFunc<TTo, TFrom> from, Serializer<TTo> serializer = null) =>
            new Mapper<TFrom, TTo>(to, from, serializer);

        public static Serializer<(T[] items, int count)> PairArray<T>() => Map(
            (in (T[], int) pair) => pair.ToArray(),
            (in T[] items) => (items, items.Length),
            Array<T>());

        public static Serializer<T[]> Array<T>() => new ConcreteArray<T>();
        public static ISerializer Array(Type type) => new ConcreteArray(type);
        public static Serializer<T> Object<T>(Func<T> construct, params IMember<T>[] members) => new ConcreteObject<T>(construct, members);
        public static Serializer<T> Object<T>(params IMember<T>[] members) => new ConcreteObject<T>(members);
        public static ISerializer Object(Type type, params IMember[] members) => new ConcreteObject(type, members);
        public static ISerializer Object(Type type)
        {
            var fields = type.InstanceFields();
            var members = fields.Select(field => Member.Reflection(field)).ToArray();
            return Object(type, members);
        }

        public static Serializer<string> String() => new ConcreteString();
        public static ISerializer Delegate(Type type) => new ConcreteDelegate(type);
        public static Serializer<List<T>> List<T>() => new ConcreteList<T>();
        public static ISerializer List(Type type) => new ConcreteList(type);
        public static Serializer<Dictionary<TKey, TValue>> Dictionary<TKey, TValue>() => new ConcreteDictionary<TKey, TValue>();
        public static ISerializer Dictionary(Type key, Type value) => new ConcreteDictionary(key, value);
        public static Serializer<(T1, T2)> Tuple<T1, T2>() => new ConcreteTuple<T1, T2>();
        public static Serializer<(T1, T2, T3)> Tuple<T1, T2, T3>() => new ConcreteTuple<T1, T2, T3>();
        public static Serializer<(T1, T2, T3, T4)> Tuple<T1, T2, T3, T4>() => new ConcreteTuple<T1, T2, T3, T4>();
        public static Serializer<(T1, T2, T3, T4, T5)> Tuple<T1, T2, T3, T4, T5>() => new ConcreteTuple<T1, T2, T3, T4, T5>();
        public static Serializer<(T1, T2, T3, T4, T5, T6)> Tuple<T1, T2, T3, T4, T5, T6>() => new ConcreteTuple<T1, T2, T3, T4, T5, T6>();
        public static Serializer<(T1, T2, T3, T4, T5, T6, T7)> Tuple<T1, T2, T3, T4, T5, T6, T7>() => new ConcreteTuple<T1, T2, T3, T4, T5, T6, T7>();
    }
}