using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Core.Providers;
using Entia.Experimental.Serialization;
using static Entia.Experimental.Serializers.Serializer;

namespace Entia.Experimental.Serializers
{
    #region System
    [Implementation(typeof(bool), typeof(BlittableObject<bool>)), Implementation(typeof(bool[]), typeof(BlittableArray<bool>))]
    [Implementation(typeof(char), typeof(BlittableObject<char>)), Implementation(typeof(char[]), typeof(BlittableArray<char>))]
    [Implementation(typeof(byte), typeof(BlittableObject<byte>)), Implementation(typeof(byte[]), typeof(BlittableArray<byte>))]
    [Implementation(typeof(sbyte), typeof(BlittableObject<sbyte>)), Implementation(typeof(sbyte[]), typeof(BlittableArray<sbyte>))]
    [Implementation(typeof(ushort), typeof(BlittableObject<ushort>)), Implementation(typeof(ushort[]), typeof(BlittableArray<ushort>))]
    [Implementation(typeof(short), typeof(BlittableObject<short>)), Implementation(typeof(short[]), typeof(BlittableArray<short>))]
    [Implementation(typeof(uint), typeof(BlittableObject<uint>)), Implementation(typeof(uint[]), typeof(BlittableArray<uint>))]
    [Implementation(typeof(int), typeof(BlittableObject<int>)), Implementation(typeof(int[]), typeof(BlittableArray<int>))]
    [Implementation(typeof(ulong), typeof(BlittableObject<ulong>)), Implementation(typeof(ulong[]), typeof(BlittableArray<ulong>))]
    [Implementation(typeof(long), typeof(BlittableObject<long>)), Implementation(typeof(long[]), typeof(BlittableArray<long>))]
    [Implementation(typeof(float), typeof(BlittableObject<float>)), Implementation(typeof(float[]), typeof(BlittableArray<float>))]
    [Implementation(typeof(double), typeof(BlittableObject<double>)), Implementation(typeof(double[]), typeof(BlittableArray<double>))]
    [Implementation(typeof(decimal), typeof(BlittableObject<decimal>)), Implementation(typeof(decimal[]), typeof(BlittableArray<decimal>))]
    [Implementation(typeof(DateTime), typeof(BlittableObject<DateTime>)), Implementation(typeof(DateTime[]), typeof(BlittableArray<DateTime>))]
    [Implementation(typeof(TimeSpan), typeof(BlittableObject<TimeSpan>)), Implementation(typeof(TimeSpan[]), typeof(BlittableArray<TimeSpan>))]
    [Implementation(typeof(Guid), typeof(BlittableObject<Guid>)), Implementation(typeof(Guid[]), typeof(BlittableArray<Guid>))]
    [Implementation(typeof(string), typeof(ConcreteString)), Implementation(typeof(string[]), typeof(ConcreteArray<string>))]
    [Implementation(typeof(Delegate), typeof(ConcreteDelegate)), Implementation(typeof(Delegate[]), typeof(ConcreteArray<Delegate>))]

    [Implementation(typeof(Nullable<>), typeof(ConcreteNullable<>))]
    [Implementation(typeof(List<>), typeof(ConcreteList<>))]
    [Implementation(typeof(Dictionary<,>), typeof(ConcreteDictionary<,>))]
    [Implementation(typeof(ValueTuple<,>), typeof(ConcreteTuple<,>))]
    [Implementation(typeof(ValueTuple<,,>), typeof(ConcreteTuple<,,>))]
    [Implementation(typeof(ValueTuple<,,,>), typeof(ConcreteTuple<,,,>))]
    [Implementation(typeof(ValueTuple<,,,,>), typeof(ConcreteTuple<,,,,>))]
    [Implementation(typeof(ValueTuple<,,,,,>), typeof(ConcreteTuple<,,,,,>))]
    [Implementation(typeof(ValueTuple<,,,,,,>), typeof(ConcreteTuple<,,,,,,>))]
    #endregion

    #region System.Reflection
    [Implementation(typeof(Assembly), typeof(AbstractAssembly)), Implementation(typeof(Assembly[]), typeof(ConcreteArray<AbstractAssembly>))]
    [Implementation(typeof(Module), typeof(AbstractModule)), Implementation(typeof(Module[]), typeof(ConcreteArray<AbstractModule>))]
    [Implementation(typeof(Type), typeof(AbstractType)), Implementation(typeof(Type[]), typeof(ConcreteArray<AbstractType>))]
    [Implementation(typeof(MethodInfo), typeof(AbstractMethod)), Implementation(typeof(MethodInfo[]), typeof(ConcreteArray<AbstractMethod>))]
    [Implementation(typeof(MemberInfo), typeof(AbstractMember)), Implementation(typeof(MemberInfo[]), typeof(ConcreteArray<AbstractMember>))]
    #endregion

    #region Entia.Core
    [Implementation(typeof(IBox), typeof(ConcreteBox))]
    [Implementation(typeof(Unit), typeof(BlittableObject<Unit>)), Implementation(typeof(Unit[]), typeof(BlittableArray<Unit>))]
    #endregion

    [Implementation(typeof(ISerializable), typeof(SerializableObject))]
    [Implementation(typeof(object), typeof(Default))]
    public interface ISerializer : ITrait
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
            var data = TypeUtility.GetData(type);
            if (type.IsArray) yield return Array(data.Element);
            else yield return Object(type);
        }
    }

    public static class Serializer
    {
        public static class Member
        {
            public static IMember<T> Field<T, TValue>(Field<T, TValue>.Getter get, Serializer<TValue> serializer = null) => new Field<T, TValue>(get, serializer);
            public static IMember<T> Property<T, TValue>(Property<T, TValue>.Getter get, Property<T, TValue>.Setter set, Serializer<TValue> serializer = null) => new Property<T, TValue>(get, set, serializer);
            public static IMember Reflection(FieldInfo field, ISerializer serializer = null) => new Experimental.Serializers.Reflection(field, serializer);
            public static IMember Reflection(PropertyInfo property, ISerializer serializer = null) => new Experimental.Serializers.Reflection(property, serializer);
        }

        public static class Blittable
        {
            public static Serializer<T[]> Array<T>() where T : unmanaged => new BlittableArray<T>();
            public static Serializer<T> Object<T>() where T : unmanaged => new BlittableObject<T>();
        }

        public static class Reflection
        {
            public static Serializer<Assembly> Assembly() => new AbstractAssembly();
            public static Serializer<Module> Module() => new AbstractModule();
            public static Serializer<Type> Type() => new AbstractType();
            public static Serializer<MethodInfo> Method() => new AbstractMethod();
            public static Serializer<MemberInfo> Member() => new AbstractMember();
        }

        public static ISerializer Any(params ISerializer[] serializers) => new Any(serializers);
        public static Serializer<T> Any<T>(params Serializer<T>[] serializers) => new Any<T>(serializers);
        public static Serializer<TFrom> Map<TFrom, TTo>(InFunc<TFrom, TTo> to, InFunc<TTo, TFrom> from, Serializer<TTo> serializer = null) => new Mapper<TFrom, TTo>(to, from, serializer);
        public static Serializer<T[]> Array<T>(Serializer<T> element = null) => new ConcreteArray<T>(element);
        public static ISerializer Array(Type element) => new ConcreteArray(element);
        public static Serializer<T> Object<T>(Func<T> construct, params IMember<T>[] members) => new ConcreteObject<T>(construct, members);
        public static Serializer<T> Object<T>(params IMember<T>[] members) => new ConcreteObject<T>(members);
        public static ISerializer Object(Type type) => new ConcreteObject(type);
        public static Serializer<ISerializable> Serializable() => new SerializableObject();
        public static Serializer<string> String() => new ConcreteString();
        public static ISerializer Delegate() => new ConcreteDelegate();
        public static Serializer<T?> Nullable<T>(Serializer<T> value = null) where T : struct => new ConcreteNullable<T>(value);
        public static Serializer<List<T>> List<T>(Serializer<T[]> values = null) => new ConcreteList<T>(values);
        public static Serializer<Dictionary<TKey, TValue>> Dictionary<TKey, TValue>(Serializer<TKey[]> keys = null, Serializer<TValue[]> values = null) => new ConcreteDictionary<TKey, TValue>(keys, values);
        public static Serializer<(T1, T2)> Tuple<T1, T2>(Serializer<T1> item1 = null, Serializer<T2> item2 = null) => new ConcreteTuple<T1, T2>(item1, item2);
        public static Serializer<(T1, T2, T3)> Tuple<T1, T2, T3>(Serializer<T1> item1 = null, Serializer<T2> item2 = null, Serializer<T3> item3 = null) => new ConcreteTuple<T1, T2, T3>(item1, item2, item3);
        public static Serializer<(T1, T2, T3, T4)> Tuple<T1, T2, T3, T4>(Serializer<T1> item1 = null, Serializer<T2> item2 = null, Serializer<T3> item3 = null, Serializer<T4> item4 = null) => new ConcreteTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        public static Serializer<(T1, T2, T3, T4, T5)> Tuple<T1, T2, T3, T4, T5>(Serializer<T1> item1 = null, Serializer<T2> item2 = null, Serializer<T3> item3 = null, Serializer<T4> item4 = null, Serializer<T5> item5 = null) => new ConcreteTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        public static Serializer<(T1, T2, T3, T4, T5, T6)> Tuple<T1, T2, T3, T4, T5, T6>(Serializer<T1> item1 = null, Serializer<T2> item2 = null, Serializer<T3> item3 = null, Serializer<T4> item4 = null, Serializer<T5> item5 = null, Serializer<T6> item6 = null) => new ConcreteTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        public static Serializer<(T1, T2, T3, T4, T5, T6, T7)> Tuple<T1, T2, T3, T4, T5, T6, T7>(Serializer<T1> item1 = null, Serializer<T2> item2 = null, Serializer<T3> item3 = null, Serializer<T4> item4 = null, Serializer<T5> item5 = null, Serializer<T6> item6 = null, Serializer<T7> item7 = null) => new ConcreteTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);

        public static Serializer<(T[] items, int count)> TupleArray<T>(Serializer<T[]> values = null) =>
            Serializer.Map((in (T[], int) pair) => pair.ToArray(), (in T[] items) => (items, items.Length), values);
    }
}